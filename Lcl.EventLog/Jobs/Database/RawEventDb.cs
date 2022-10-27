/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Data.Sqlite;

using Dapper;

using Lcl.EventLog.Utilities;
using System.Diagnostics.Eventing.Reader;

namespace Lcl.EventLog.Jobs.Database
{
  /// <summary>
  /// Interface for accessing a raw event database
  /// </summary>
  public class RawEventDb
  {
    /// <summary>
    /// Create a new RawEventDb
    /// </summary>
    public RawEventDb(string fileName, bool allowWrite, bool allowCreate = false)
    {
      FileName = Path.GetFullPath(fileName);
      AllowWrite = allowWrite;
      AllowCreate = allowCreate;
      if(!AllowCreate && !File.Exists(FileName))
      {
        throw new FileNotFoundException(
          $"Attempt to open a nonexisting DB in no-create mode");
      }
    }

    /// <summary>
    /// The database filename
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Allow opening the DB in writable mode
    /// </summary>
    public bool AllowWrite { get; }

    /// <summary>
    /// Allow opening the DB in DDL mode
    /// </summary>
    public bool AllowCreate { get; }

    /// <summary>
    /// Open a new database connection. This may create the database file if it didn't
    /// exist yet.
    /// </summary>
    /// <param name="writable">
    /// Enable writing to the db (i.e. not readonly)
    /// </param>
    /// <param name="create">
    /// Enable creating the database file if it didn't exist yet
    /// </param>
    /// <returns>
    /// The database connection, already opened
    /// </returns>
    public OpenDb Open(bool writable, bool create = false)
    {
      if(!writable && create)
      {
        throw new ArgumentException(
          $"Invalid argument combination. create=true should imply writable=true");
      }
      if(writable && !AllowWrite)
      {
        throw new InvalidOperationException(
          $"Attempt to open a read-only database for writing");
      }
      if(create && !AllowCreate)
      {
        throw new InvalidOperationException(
          $"Attempt to open a no-DDL database in create mode");
      }
      SqliteOpenMode mode;
      if(create)
      {
        mode = SqliteOpenMode.ReadWriteCreate;
      }
      else if(writable)
      {
        mode = SqliteOpenMode.ReadWrite;
      }
      else
      {
        mode = SqliteOpenMode.ReadOnly;
      }
      if(!File.Exists(FileName))
      {
        if(create)
        {
          var dir = Path.GetDirectoryName(FileName);
          if(dir == null)
          {
            throw new InvalidOperationException("Bad db name");
          }
          if(!Directory.Exists(dir))
          {
            Directory.CreateDirectory(dir);
          }
        }
        else
        {
          throw new InvalidOperationException(
            $"The database does not exist and creation is disabled: {FileName}");
        }
      }
      var builder = new SqliteConnectionStringBuilder {
        DataSource = FileName,
        Mode = mode,
        ForeignKeys = true,
      };
      var conn = new SqliteConnection(builder.ConnectionString);
      return new OpenDb(conn, writable, create);
    }

    /// <summary>
    /// Represents an Open Sqlite connection
    /// </summary>
    public class OpenDb: IDisposable
    {
      internal OpenDb(SqliteConnection conn, bool canWrite, bool canCreate)
      {
        Connection = conn;
        CanWrite = canWrite;
        CanCreate = canCreate;
        Connection.Open();
      }

      /// <summary>
      /// Writing is allowed
      /// </summary>
      public bool CanWrite { get; }

      /// <summary>
      /// DDL is allowed
      /// </summary>
      public bool CanCreate { get; }

      internal SqliteConnection Connection { get; }

      /// <summary>
      /// Dispose this object and the db connection it wraps
      /// </summary>
      public void Dispose()
      {
        Connection.Close();
        Connection.Dispose();
      }

      /// <summary>
      /// Ensure the database tables exist.
      /// </summary>
      public void DbInit()
      {
        InitTables();
      }

      /// <summary>
      /// Update the database from the named event log.
      /// </summary>
      /// <param name="eventLogName">
      /// The name of the event log
      /// </param>
      /// <param name="cap">
      /// The maximum number of new records to import
      /// </param>
      /// <returns>
      /// The number of records inserted. If this is equal to <paramref name="cap"/>
      /// there may be more records to import
      /// </returns>
      public int UpdateFrom(
        string eventLogName,
        int cap = Int32.MaxValue)
      {
        var aboveRid = MaxRecordId();
        var ers = new EventRecordSource(eventLogName);
        return PutEvents(ers.ReadRecords(aboveRid), cap, ConflictMode.Default);
      }

      /// <summary>
      /// Return the number of events for each unique (eventId, taskId) combination
      /// </summary>
      public IReadOnlyList<(int EventId, int TaskId, int Total)> EventTaskCounts()
      {
        return Connection.Query<(int EventId, int TaskId, int Total)>(@"
SELECT eid AS EventId, task AS TaskId, Count(*) AS Total
FROM Events
GROUP BY eid, task
ORDER BY eid, task").ToList().AsReadOnly();
      }

      /// <summary>
      /// Return an overview of the DB content (using one row per eventId/task combination)
      /// </summary>
      public IReadOnlyList<DbOverview> GetOverview()
      {
        return Connection.Query<DbOverview>(@"
SELECT
  t.eid as eventId,
  t.task as taskId,
  t.description AS taskLabel,
  s.minversion AS minVersion,
  s.enabled AS isEnabled,
  COUNT(*) AS eventCount,
  MIN(e.rid) AS minRid, 
  MAX(e.rid) AS maxRid,
  MIN(e.ts) AS eticksMin,
  MAX(e.ts) AS eticksMax,
  SUM(LENGTH(e.xml)) AS xmlSize
FROM EventState s, Tasks t, Events e
WHERE s.eid = t.eid AND e.eid = t.eid AND e.task = t.task
GROUP BY t.eid, t.task
ORDER BY t.eid, t.task").ToList().AsReadOnly();
      }

      /// <summary>
      /// Insert a batch of records into the database
      /// </summary>
      /// <param name="records">
      /// The sequence of records to import
      /// </param>
      /// <param name="cap">
      /// The maximum number of records to import
      /// (potentially leaving part of the input sequence
      /// unhandled)
      /// </param>
      /// <param name="conflictHandling">
      /// Determines how handle insertion conflicts
      /// </param>
      public int PutEvents(
        IEnumerable<EventLogRecord> records,
        int cap = Int32.MaxValue,
        ConflictMode conflictHandling = ConflictMode.Default)
      {
        var n = 0;
        using(var eij = new EventImportJob(this))
        {
          eij.ConflictHandling = conflictHandling;
          foreach(var record in records)
          {
            if(eij.ProcessEvent(record))
            {
              n++;
              if(n >= cap)
              {
                break;
              }
            }
          }
          eij.Commit(true);
        }
        return n;
      }

      /// <summary>
      /// Read all event states from the DB
      /// </summary>
      public IEnumerable<EventStateRow> ReadEventStates()
      {
        return Connection.Query<EventStateRow>(@"
SELECT eid, minversion, enabled
FROM EventState");
      }

      /// <summary>
      /// Insert or replace a full event state row
      /// </summary>
      public int PutEventState(int eid, int minversion, bool enabled)
      {
        return Connection.Execute(@"
INSERT OR REPLACE INTO EventState (eid, minversion, enabled)
  VALUES (@Eid, @MinVersion, @Enabled)
", new { Eid = eid, MinVersion = minversion, Enabled = enabled ? 1 : 0 });
      }

      /// <summary>
      /// Set the enabled field of an event state row (or insert a new row)
      /// </summary>
      public int EnableEventState(int eid, bool enabled)
      {
        // ref https://www.sqlite.org/lang_upsert.html
        return Connection.Execute(@"
INSERT INTO EventState (eid, enabled)
  VALUES (@Eid, @Enabled)
  ON CONFLICT(eid) DO UPDATE SET enabled=excluded.enabled
", new { Eid = eid, Enabled = enabled ? 1 : 0 });
      }

      /// <summary>
      /// Set the minversion field of an event state row (or insert a new row)
      /// </summary>
      public int SetMinVersion(int eid, int minversion)
      {
        return Connection.Execute(@"
INSERT INTO EventState (eid, minversion)
  VALUES (@Eid, @MinVersion)
  ON CONFLICT(eid) DO UPDATE SET minversion=excluded.minversion
", new { Eid = eid, MinVersion = minversion });
      }

      /// <summary>
      /// Return all rows of the tasks table
      /// </summary>
      public IEnumerable<TaskRow> ReadTasks()
      {
        return Connection.Query<TaskRow>(@"
SELECT eid, task, description
FROM Tasks
ORDER BY eid, task"); // need to order explicitly since PK is not "INTEGER PRIMARY KEY"
      }

      /// <summary>
      /// Store a new task row or update an existing one
      /// </summary>
      public int PutTask(int eid, int task, string? description)
      {
        return Connection.Execute(@"
INSERT INTO Tasks (eid, task, description)
  VALUES (@Eid, @Task, @Description)
  ON CONFLICT(eid, task) DO UPDATE SET description=excluded.description
", new { Eid = eid, Task = task, Description = description });
      }

      /// <summary>
      /// Read events, filtered by the given query parameters.
      /// Timestamps are specified in Epoch Ticks (use TimeUtil for conversions)
      /// Use <see cref="ReadEvents"/> for the equivalent that uses
      /// DateTime instead.
      /// </summary>
      /// <param name="ridMin">
      /// Minimum Record ID to match
      /// </param>
      /// <param name="ridMax">
      /// Maximum Record ID to match
      /// </param>
      /// <param name="eid">
      /// The event ID to match (or all event IDs)
      /// </param>
      /// <param name="tMin">
      /// The minimum time stamp to match (in epoch-ticks)
      /// </param>
      /// <param name="tMax">
      /// The maximum time stamp to match (in epoch-ticks)
      /// </param>
      /// <returns>
      /// The matching records
      /// </returns>
      /// <remarks>
      /// <para>
      /// This method is named ReadEventsTicks instead of ReadEvents to resolve
      /// overload ambiguity when no time stamp limits are specified.
      /// </para>
      /// </remarks>
      public IEnumerable<EventRow> ReadEventsTicks(
        long? ridMin = null,
        long? ridMax = null,
        int? eid = null,
        long? tMin = null,
        long? tMax = null)
      {
        var q = @"
SELECT rid, eid, task, ts, ver, xml
FROM Events";
        var conditions = new List<string>();
        if(ridMin != null)
        {
          conditions.Add("rid >= @RidMin");
        }
        if(ridMax != null)
        {
          conditions.Add("rid <= @RidMax");
        }
        if(eid != null)
        {
          conditions.Add("eid = @Eid");
        }
        if(tMin != null)
        {
          conditions.Add("ts >= @TMin");
        }
        if(tMax != null)
        {
          conditions.Add("ts <= @TMax");
        }
        if(conditions.Count > 0)
        {
          var condition = @"
WHERE " + String.Join(@"
  AND ", conditions);
          q = q + condition;
        }
        return Connection.Query<EventRow>(q, new {
          RidMin = ridMin,
          RidMax = ridMax,
          Eid = eid,
          TMin = tMin,
          TMax = tMax
        });
      }

      /// <summary>
      /// Like <see cref="ReadEventsTicks(long?, long?, int?, long?, long?)"/>,
      /// but only return matching record IDs
      /// </summary>
      public IEnumerable<long> ReadEventIdsTicks(
        long? ridMin = null,
        long? ridMax = null,
        int? eid = null,
        long? tMin = null,
        long? tMax = null)
      {
        var q = @"
SELECT rid
FROM Events";
        var conditions = new List<string>();
        if(ridMin != null)
        {
          conditions.Add("rid >= @RidMin");
        }
        if(ridMax != null)
        {
          conditions.Add("rid <= @RidMax");
        }
        if(eid != null)
        {
          conditions.Add("eid = @Eid");
        }
        if(tMin != null)
        {
          conditions.Add("ts >= @TMin");
        }
        if(tMax != null)
        {
          conditions.Add("ts <= @TMax");
        }
        if(conditions.Count > 0)
        {
          var condition = @"
WHERE " + String.Join(@"
  AND ", conditions);
          q = q + condition;
        }
        return Connection.Query<long>(q, new {
          RidMin = ridMin,
          RidMax = ridMax,
          Eid = eid,
          TMin = tMin,
          TMax = tMax
        });
      }

      /// <summary>
      /// Read events, filtered by the given query parameters.
      /// Timestamps are specified as DateTime, with a Kind of Utc or Local.
      /// Use <see cref="ReadEventsTicks"/> for the equivalent that uses
      /// epoch ticks instead.
      /// </summary>
      /// <param name="ridMin">
      /// Minimum Record ID to match
      /// </param>
      /// <param name="ridMax">
      /// Maximum Record ID to match
      /// </param>
      /// <param name="eid">
      /// The event ID to match (or all event IDs)
      /// </param>
      /// <param name="utcMin">
      /// The minimum time stamp to match. The Kind must be UTC or Local, not Unspecified.
      /// </param>
      /// <param name="utcMax">
      /// The maximum time stamp to match. The Kind must be UTC or Local, not Unspecified.
      /// </param>
      /// <returns>
      /// The matching records
      /// </returns>
      public IEnumerable<EventRow> ReadEvents(
        long? ridMin = null,
        long? ridMax = null,
        int? eid = null,
        DateTime? utcMin = null,
        DateTime? utcMax = null)
      {
        long? tMin = utcMin.HasValue ? TimeUtil.TicksSinceEpoch(utcMin.Value) : null;
        long? tMax = utcMax.HasValue ? TimeUtil.TicksSinceEpoch(utcMax.Value) : null;
        return ReadEventsTicks(ridMin, ridMax, eid, tMin, tMax);
      }

      /// <summary>
      /// Return a single record (or null if it does not exist)
      /// </summary>
      /// <param name="rid">
      /// The record ID identifying the record
      /// </param>
      /// <returns>
      /// the requested record or null if not found
      /// </returns>
      public EventRow? ReadEvent(long rid)
      {
        var q = @"
SELECT rid, eid, task, ts, ver, xml
FROM Events
WHERE rid = @Rid";
        return Connection.QuerySingleOrDefault<EventRow>(q, new { Rid = rid });
      }

      /// <summary>
      /// Insert a new event record in the Events table.
      /// Does not affect the Tasks and EventState tables.
      /// Normally you should call this inside a transaction only.
      /// </summary>
      public int PutEvent(long rid, int eid, int task, long ts, int ver, string xml, ConflictMode conflict = ConflictMode.Default)
      {
        var cmd =
          conflict switch {
            ConflictMode.Default => "INSERT",
            ConflictMode.Replace => "INSERT OR REPLACE",
            ConflictMode.Ignore => "INSERT OR IGNORE",
            _ => throw new InvalidOperationException("Unknown conflict mode"),
          };

        return Connection.Execute(cmd + @"
INTO Events (rid, eid, task, ts, ver, xml)
VALUES (@Rid, @Eid, @Task, @Ts, @Ver, @Xml)", new {
          Rid = rid,
          Eid = eid,
          Task = task,
          Ts = ts,
          Ver = ver,
          Xml = xml
        });
      }

      /// <summary>
      /// Insert a new event record in the Events table.
      /// Does not affect the Tasks and EventState tables.
      /// Normally you should call this inside a transaction only.
      /// </summary>
      public int PutEvent(long rid, int eid, int task, DateTime utc, int ver, string xml, ConflictMode conflict = ConflictMode.Default)
      {
        var ts = TimeUtil.TicksSinceEpoch(utc);
        return PutEvent(rid, eid, task, ts, ver, xml, conflict);
      }

      /// <summary>
      /// Return the maximum record ID in the Events table (returning null if the table is empty)
      /// </summary>
      public long? MaxRecordId()
      {
        return Connection.ExecuteScalar<long?>(@"
SELECT MAX(rid)
FROM Events");
      }

      private void InitTables()
      {
        var q = Connection.Query<string>(
          @"SELECT name FROM sqlite_master WHERE type='table'");
        var names = new HashSet<string>(q);
        if(!names.Contains("EventState"))
        {
          CreateEventStateTable();
        }
        if(!names.Contains("Tasks"))
        {
          CreateTasksTable();
        }
        if(!names.Contains("Events"))
        {
          CreateEventsTable();
        }
      }

      private void CreateEventStateTable()
      {
        if(!CanWrite || !CanCreate)
        {
          throw new InvalidOperationException(
            "Cannot create tables. The database connection is in no-create mode.");
        }
        var sql = @"
CREATE TABLE IF NOT EXISTS EventState (
  eid INTEGER PRIMARY KEY,
  minversion INTEGER NOT NULL DEFAULT 0,
  enabled INTEGER NOT NULL DEFAULT 1 
);";
        Connection.Execute(sql);
      }

      private void CreateTasksTable()
      {
        if(!CanWrite || !CanCreate)
        {
          throw new InvalidOperationException(
            "Cannot create tables. The database connection is in no-create mode.");
        }
        var sql = @"
CREATE TABLE IF NOT EXISTS Tasks (
  eid INTEGER NOT NULL,
  task INTEGER NOT NULL,
  description TEXT NULL,
  PRIMARY KEY (eid, task)
);";
        Connection.Execute(sql);
      }

      private void CreateEventsTable()
      {
        if(!CanWrite || !CanCreate)
        {
          throw new InvalidOperationException(
            "Cannot create tables. The database connection is in no-create mode.");
        }
        var sql = @"
CREATE TABLE IF NOT EXISTS Events (
  rid INTEGER PRIMARY KEY,
  eid INTEGER NOT NULL,
  task INTEGER NOT NULL,
  ts INTEGER NOT NULL,
  ver INTEGER NOT NULL,
  xml TEXT NOT NULL 
);";
        /*,
          FOREIGN KEY (eid) REFERENCES EventState (eid),
          FOREIGN KEY (eid, task) REFERENCES Tasks (eid, task) */
        Connection.Execute(sql);
      }

    }

  }
}
