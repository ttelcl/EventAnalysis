/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Dapper;

using Lcl.EventLog.Utilities;

using Microsoft.Data.Sqlite;

namespace Lcl.EventLog.Jobs.Database
{

  /// <summary>
  /// Represents an Open Sqlite connection
  /// </summary>
  public class OpenDbV2: IDisposable
  {
    internal OpenDbV2(
      RawEventDbV2 owner,
      SqliteConnection conn,
      bool canWrite,
      bool canCreate)
    {
      Owner = owner;
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
    /// The database descriptor owning this opened DB
    /// </summary>
    public RawEventDbV2 Owner { get; }

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
    /// <returns>
    /// True if any DB objects were created, false if they already existed.
    /// </returns>
    public bool DbInit()
    {
      var tablesCreated = InitTables();
      var viewCreated = InitCompositeView();
      return tablesCreated || viewCreated;
    }

    /// <summary>
    /// Return the names of the tables in the DB
    /// </summary>
    public IEnumerable<string> DbTables()
    {
      return Connection.Query<string>(@"
SELECT name
FROM sqlite_master
WHERE type = 'table'");
    }

    /// <summary>
    /// Return the names of the views in the DB
    /// </summary>
    public IEnumerable<string> DbViews()
    {
      return Connection.Query<string>(@"
SELECT name
FROM sqlite_master
WHERE type = 'view'");
    }

    /// <summary>
    /// Return the maximum record ID in the Events table (returning null if the table is empty)
    /// </summary>
    public long? MaxRecordId()
    {
      return Connection.ExecuteScalar<long?>(@"
SELECT MAX(rid)
FROM EventHeader");
    }

    /// <summary>
    /// Return the minimum record ID in the Events table (returning null if the table is empty)
    /// </summary>
    public long? MinRecordId()
    {
      return Connection.ExecuteScalar<long?>(@"
SELECT MIN(rid)
FROM EventHeader");
    }

    /// <summary>
    /// Return all provider info records 
    /// </summary>
    public IEnumerable<ProviderInfoRow> AllProviderInfoRows()
    {
      return Connection.Query<ProviderInfoRow>(@"
SELECT prvid, prvname, prvguid
FROM ProviderInfo");
    }

    /// <summary>
    /// Lookup a single provider row by name (returning null if not found)
    /// </summary>
    public ProviderInfoRow? FindProvider(string providerName)
    {
      return Connection.QuerySingleOrDefault<ProviderInfoRow>(@"
SELECT prvid, prvname, prvguid
FROM ProviderInfo
WHERE prvname = @PrvName", new { PrvName = providerName });
    }

    /// <summary>
    /// Lookup a single provider row by internal id (returning null if not found)
    /// </summary>
    public ProviderInfoRow? FindProvider(int providerId)
    {
      return Connection.QuerySingleOrDefault<ProviderInfoRow>(@"
SELECT prvid, prvname, prvguid
FROM ProviderInfo
WHERE prvid = @PrvId", new { PrvId = providerId });
    }

    /// <summary>
    /// Lookup the provider info for the event header record
    /// </summary>
    public ProviderInfoRow? FindProvider(IProviderInfoKey ipik)
    {
      return FindProvider(ipik.ProviderId);
    }

    /// <summary>
    /// Return all TaskInfo records
    /// </summary>
    public IEnumerable<TaskInfoRow> AllTaskInfoRows()
    {
      return Connection.Query<TaskInfoRow>(@"
SELECT eid, ever, task, prvid, taskdesc
FROM TaskInfo");
    }

    /// <summary>
    /// Lookup a single task info record (returning null if not found)
    /// </summary>
    public TaskInfoRow? FindTask(ITaskInfoKey itik)
    {
      return Connection.QuerySingleOrDefault<TaskInfoRow>(@"
SELECT eid, ever, task, prvid, taskdesc
FROM TaskInfo
WHERE eid=@Eid AND ever=@Ever AND task=@TaskId AND prvid=@PrvId",
      new {
        Eid = itik.EventId,
        Ever = itik.EventVersion,
        TaskId = itik.TaskId,
        PrvId = itik.ProviderId,
      });
    }

    /// <summary>
    /// Return all OperationInfo records
    /// </summary>
    public IEnumerable<OperationInfoRow> AllOperationInfoRows()
    {
      return Connection.Query<OperationInfoRow>(@"
SELECT eid, ever, task, prvid, opid, opdesc
FROM OperationInfo");
    }

    /// <summary>
    /// Lookup a single operation info record (returning null if not found)
    /// </summary>
    public OperationInfoRow? FindOperation(IOperationInfoKey ioik)
    {
      return Connection.QuerySingleOrDefault<OperationInfoRow>(@"
SELECT eid, ever, task, prvid, opid, opdesc
FROM OperationInfo
WHERE eid=@Eid AND ever=@Ever AND task=@TaskId AND prvid=@PrvId AND opid=@OpId",
      new {
        Eid = ioik.EventId,
        Ever = ioik.EventVersion,
        TaskId = ioik.TaskId,
        PrvId = ioik.ProviderId,
        OpId = ioik.OperationId,
      });
    }

    /// <summary>
    /// Query the EventHeaders table
    /// </summary>
    /// <param name="ridMin">Minimum Record ID</param>
    /// <param name="ridMax">Maximum Record ID</param>
    /// <param name="eid">The exact Event ID to match</param>
    /// <param name="tMin">Minimum event timestamp as epoch ticks</param>
    /// <param name="tMax">Maximum event timestamp as epoch ticks</param>
    /// <param name="prvid">The exact internal provider ID</param>
    /// <param name="reverse">Return results in reverse RID order when true.</param>
    /// <returns>A sequence of EventHeaderRow objects</returns>
    public IEnumerable<EventHeaderRow> QueryEventHeaders(
      long? ridMin = null,
      long? ridMax = null,
      int? eid = null,
      long? tMin = null,
      long? tMax = null,
      int? prvid = null,
      bool reverse = false)
    {
      var q = @"
SELECT rid, stamp, eid, ever, task, prvid, opid
FROM EventHeader";
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
        conditions.Add("stamp >= @TMin");
      }
      if(tMax != null)
      {
        conditions.Add("stamp <= @TMax");
      }
      if(prvid != null)
      {
        conditions.Add("prvid = @PrvId");
      }
      if(conditions.Count > 0)
      {
        var condition = @"
WHERE " + String.Join(@"
  AND ", conditions);
        q += condition;
      }
      q += @"
ORDER BY rid " + (reverse ? "DESC" : "ASC");

      return Connection.Query<EventHeaderRow>(q, new {
        RidMin = ridMin,
        RidMax = ridMax,
        Eid = eid,
        TMin = tMin,
        TMax = tMax,
        PrvId = prvid,
      });
    }

    /// <summary>
    /// Query the joined EventHeaders + EventXml tables
    /// </summary>
    /// <param name="ridMin">Minimum Record ID</param>
    /// <param name="ridMax">Maximum Record ID</param>
    /// <param name="eid">The exact Event ID to match</param>
    /// <param name="tMin">Minimum event timestamp as epoch ticks</param>
    /// <param name="tMax">Maximum event timestamp as epoch ticks</param>
    /// <param name="prvid">The exact internal provider ID</param>
    /// <param name="reverse">Return results in reverse RID order when true.</param>
    /// <returns></returns>
    public IEnumerable<EventViewRow> QueryEvents(
      long? ridMin = null,
      long? ridMax = null,
      int? eid = null,
      long? tMin = null,
      long? tMax = null,
      int? prvid = null,
      bool reverse = false)
    {
      var q = @"
SELECT h.rid, h.stamp, h.eid, h.ever, h.task, h.prvid, h.opid, x.xml
FROM EventHeader h 
INNER JOIN EventXml x on x.rid = h.rid";
      var conditions = new List<string>();
      if(ridMin != null)
      {
        conditions.Add("h.rid >= @RidMin");
      }
      if(ridMax != null)
      {
        conditions.Add("h.rid <= @RidMax");
      }
      if(eid != null)
      {
        conditions.Add("eid = @Eid");
      }
      if(tMin != null)
      {
        conditions.Add("stamp >= @TMin");
      }
      if(tMax != null)
      {
        conditions.Add("stamp <= @TMax");
      }
      if(prvid != null)
      {
        conditions.Add("prvid = @PrvId");
      }
      if(conditions.Count > 0)
      {
        var condition = @"
WHERE " + String.Join(@"
  AND ", conditions);
        q += condition;
      }
      q += @"
ORDER BY h.rid " + (reverse ? "DESC" : "ASC");

      return Connection.Query<EventViewRow>(q, new {
        RidMin = ridMin,
        RidMax = ridMax,
        Eid = eid,
        TMin = tMin,
        TMax = tMax,
        PrvId = prvid,
      });
    }

    /// <summary>
    /// Query the EventHeaders table, returning only the record IDs
    /// </summary>
    /// <param name="ridMin">Minimum Record ID</param>
    /// <param name="ridMax">Maximum Record ID</param>
    /// <param name="eid">The exact Event ID to match</param>
    /// <param name="tMin">Minimum event timestamp as epoch ticks</param>
    /// <param name="tMax">Maximum event timestamp as epoch ticks</param>
    /// <param name="prvid">The exact internal provider ID</param>
    /// <param name="reverse">Return results in reverse RID order when true.</param>
    /// <returns>A sequence of record IDs</returns>
    public IEnumerable<long> QueryEventIds(
      long? ridMin = null,
      long? ridMax = null,
      int? eid = null,
      long? tMin = null,
      long? tMax = null,
      int? prvid = null,
      bool reverse = false)
    {
      var q = @"
SELECT rid
FROM EventHeader";
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
        conditions.Add("stamp >= @TMin");
      }
      if(tMax != null)
      {
        conditions.Add("stamp <= @TMax");
      }
      if(prvid != null)
      {
        conditions.Add("prvid = @PrvId");
      }
      if(conditions.Count > 0)
      {
        var condition = @"
WHERE " + String.Join(@"
  AND ", conditions);
        q += condition;
      }
      q += @"
ORDER BY rid " + (reverse ? "DESC" : "ASC");

      return Connection.Query<long>(q, new {
        RidMin = ridMin,
        RidMax = ridMax,
        Eid = eid,
        TMin = tMin,
        TMax = tMax,
        PrvId = prvid,
      });
    }

    /// <summary>
    /// Get an overview of the distinct provider-event-task-operation combinations
    /// appearing in the database. Optionally include event counts.
    /// </summary>
    /// <param name="includeEvents">
    /// When true, also event counts are included (at a heavy performance cost).
    /// When false, event counts are reported as 0.
    /// </param>
    /// <returns>
    /// A sequence of DbOverviewRow objects, sorted by
    /// (ProviderName, ProviderId, EventId, EventVersion, Taskid, OpcodeId).
    /// </returns>
    public IEnumerable<DbOverviewRow> GetOverview(bool includeEvents)
    {
      if(includeEvents)
      {
        return Connection.Query<DbOverviewRow>(@"
SELECT o.prvid, o.eid, o.ever, o.task, o.opid, p.prvname, t.taskdesc, o.opdesc, COUNT(h.rid) as eventcount
FROM OperationInfo o
LEFT JOIN TaskInfo t USING(eid, ever, task, prvid)
LEFT JOIN ProviderInfo p USING(prvid)
LEFT JOIN EventHeader h USING(eid)
GROUP BY o.prvid, o.eid, o.ever, o.task, o.opid, p.prvname, t.taskdesc, o.opdesc
ORDER BY p.prvname, p.prvid, o.eid, o.ever, o.task, o.opid");
      }
      else
      {
        return Connection.Query<DbOverviewRow>(@"
SELECT o.prvid, o.eid, o.ever, o.task, o.opid, p.prvname, t.taskdesc, o.opdesc
FROM OperationInfo o
LEFT JOIN TaskInfo t USING(eid, ever, task, prvid)
LEFT JOIN ProviderInfo p USING(prvid)
ORDER BY p.prvname, p.prvid, o.eid, o.ever, o.task, o.opid");
      }
    }

    /// <summary>
    /// Lookup an EventHeader record (use <see cref="FindEvent(long)"/> if you
    /// also want the XML)
    /// </summary>
    public EventHeaderRow? FindEventHeader(long rid)
    {
      return Connection.QuerySingleOrDefault<EventHeaderRow>(@"
SELECT rid, stamp, eid, ever, task, prvid, opid
FROM EventHeader
WHERE rid=@RecordId",
      new {
        RecordId = rid,
      });
    }

    /// <summary>
    /// Lookup an EventHeader record (use <see cref="FindEvent(IEventKey)"/> if you
    /// also want the XML)
    /// </summary>
    public EventHeaderRow? FindEventHeader(IEventKey iek)
    {
      return FindEventHeader(iek.RecordId);
    }

    /// <summary>
    /// Lookup an EventXml record
    /// </summary>
    public EventXmlRow? FindEventXml(long rid)
    {
      return Connection.QuerySingleOrDefault<EventXmlRow>(@"
SELECT rid, xml
FROM EventXml
WHERE rid=@RecordId",
      new {
        RecordId = rid,
      });
    }

    /// <summary>
    /// Lookup an EventXml record
    /// </summary>
    public EventXmlRow? FindEventXml(IEventKey iek)
    {
      return FindEventXml(iek.RecordId);
    }

    /// <summary>
    /// Find a composite event record from the EventHeader and EventXml
    /// tables (returning null if either record is not found)
    /// </summary>
    public EventViewRow? FindEvent(long rid)
    {
      return Connection.QuerySingleOrDefault<EventViewRow>(@"
SELECT h.rid, h.stamp, h.eid, h.ever, h.task, h.prvid, h.opid, x.xml
FROM EventHeader h 
INNER JOIN EventXml x on x.rid = h.rid
WHERE h.rid=@RecordId",
      new {
        RecordId = rid,
      });
    }

    /// <summary>
    /// Find a composite event record from the EventHeader and EventXml
    /// tables (returning null if either record is not found)
    /// </summary>
    public EventViewRow? FindEvent(IEventKey iek)
    {
      return FindEvent(iek.RecordId);
    }

    /// <summary>
    /// Update the database from the named event log.
    /// </summary>
    /// <param name="eventLogName">
    /// The name of the event log (a.k.a. "channel")
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
      var updateCount = PutEvents(ers.ReadRecords(aboveRid), cap);
      return updateCount;
    }

    /// <summary>
    /// Insert a batch of event records into the database (including updates
    /// to the ProviderInfo, TaskInfo, and OperationInfo tables)
    /// </summary>
    /// <param name="records">
    /// The sequence of records to import
    /// </param>
    /// <param name="cap">
    /// The maximum number of records to import
    /// (potentially leaving part of the input sequence
    /// unhandled)
    /// </param>
    /// <returns>
    /// Returns the number of records inserted.
    /// </returns>
    public int PutEvents(
      IEnumerable<EventLogRecord> records,
      int cap = Int32.MaxValue)
    {
      var n = 0;
      using(var eij = new EventImportJob2(this))
      {
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
        eij.Commit();
      }
      return n;
    }

    /// <summary>
    /// Insert a new ProviderInfo record. This operation fails
    /// if the ProviderId or ProviderName already exists
    /// </summary>
    public int InsertProviderInfoRow(int prvId, string prvName, string? prvGuid)
    {
      var sql = @"
INSERT INTO ProviderInfo (prvid, prvname, prvguid)
VALUES (@PrvId, @PrvName, @PrvGuid)";
      return Connection.Execute(sql, new {
        PrvId = prvId,
        PrvName = prvName,
        PrvGuid = prvGuid
      });
    }

    /// <summary>
    /// Insert a new ProviderInfoRow
    /// </summary>
    public int InsertProviderInfoRow(ProviderInfoRow pir)
    {
      return InsertProviderInfoRow(pir.ProviderId, pir.ProviderName, pir.ProviderGuid);
    }

    /// <summary>
    /// Insert a row into the TaskInfo table
    /// </summary>
    public int InsertTaskInfoRow(int eid, int ever, int task, int prvid, string? taskdesc)
    {
      return Connection.Execute(@"
INSERT INTO TaskInfo (eid, ever, task, prvid, taskdesc)
VALUES (@Eid, @Ever, @Task, @PrvId, @TaskDesc)",
        new {
          Eid = eid,
          Ever = ever,
          Task = task,
          PrvId = prvid,
          TaskDesc = taskdesc,
        });
    }

    /// <summary>
    /// Insert a row into the TaskInfo table
    /// </summary>
    public int InsertTaskInfoRow(TaskInfoRow tir)
    {
      return InsertTaskInfoRow(tir.EventId, tir.EventVersion, tir.TaskId, tir.ProviderId, tir.TaskDescription);
    }

    /// <summary>
    /// Insert a row into the OperationInfo table
    /// </summary>
    public int InsertOperationInfoRow(int eid, int ever, int task, int prvid, int opid, string? opdesc)
    {
      return Connection.Execute(@"
INSERT INTO OperationInfo (eid, ever, task, prvid, opid, opdesc)
VALUES (@Eid, @Ever, @Task, @PrvId, @OpId, @OpDesc)",
        new {
          Eid = eid,
          Ever = ever,
          Task = task,
          PrvId = prvid,
          OpId = opid,
          OpDesc = opdesc,
        });
    }

    /// <summary>
    /// Insert a row into the OperationInfo table
    /// </summary>
    public int InsertOperationInfoRow(OperationInfoRow oir)
    {
      return InsertOperationInfoRow(oir.EventId, oir.EventVersion, oir.TaskId, oir.ProviderId, oir.OperationId, oir.OperationDescription);
    }

    /// <summary>
    /// Insert an Event Header Row 
    /// </summary>
    internal int InsertEventHeaderRow(
      long rid, long stamp, int eid, int ever, int task, int prvid, int opid)
    {
      return Connection.Execute(@"
INSERT INTO EventHeader (rid, stamp, eid, ever, task, prvid, opid)
VALUES (@Rid, @Stamp, @Eid, @Ever, @Task, @PrvId, @OpId)",
        new {
          Rid = rid,
          Stamp = stamp,
          Eid = eid,
          Ever = ever,
          Task = task,
          PrvId = prvid,
          OpId = opid
      });
    }

    internal int InsertEventXml(long rid, string xml)
    {
      return Connection.Execute(@"
INSERT INTO EventXml (rid, xml)
VALUES (@Rid, @Xml)"
      ,
        new {
          Rid = rid,
          Xml = xml,
        });
    }

    /// <summary>
    /// Insert an event record (header + xml). Does not update task, operation or provider
    /// tables.
    /// </summary>
    public void InsertEvent(
      long rid, long stamp, int eid, int ever, int task, int prvid, int opid, string xml)
    {
      InsertEventHeaderRow(rid, stamp, eid, ever, task, prvid, opid);
      InsertEventXml(rid, xml);
    }

    /// <summary>
    /// Insert an event record (header + xml). Does not update task, operation or provider
    /// tables.
    /// </summary>
    public void InsertEvent(EventHeaderRow ehr, string xml)
    {
      InsertEvent(
        ehr.RecordId, ehr.Stamp, ehr.EventId, ehr.EventVersion, ehr.TaskId,
        ehr.ProviderId, ehr.OperationId, xml);
    }

    private bool InitCompositeView()
    {
      var viewNames = DbViews().ToList();
      if(viewNames.Contains("Composite"))
      {
        return false;
      }
      else
      {
        Connection.Execute(@"
CREATE VIEW Composite AS
	SELECT h.*, p.prvname, x.xml
	FROM EventHeader h
	JOIN ProviderInfo p USING (prvid)
	JOIN EventXml x USING (rid)");
        return true;
      }
    }
    
    private bool InitTables()
    {
      if(!CanWrite || !CanCreate)
      {
        throw new InvalidOperationException(
          "Cannot create tables. The database connection is in no-create mode.");
      }
      var tableNames = DbTables().ToList();
      if(tableNames.Contains("EventXml"))
      {
        return false;
      }
      else
      {
        Connection.Execute(__dbCreateSql);
        return true;
      }
    }

    private const string __dbCreateSql = @"
CREATE TABLE ProviderInfo (
	prvid INTEGER PRIMARY KEY,
	prvname TEXT NOT NULL,
	prvguid TEXT NULL,
	UNIQUE (prvname)
);

CREATE TABLE TaskInfo (
	eid INTEGER NOT NULL,
	ever INTEGER NOT NULL,
	task INTEGER NOT NULL,
	prvid INTEGER NOT NULL,
	taskdesc TEXT NULL,
	UNIQUE (eid, ever, task, prvid)
);

CREATE TABLE OperationInfo (
	eid INTEGER NOT NULL,
	ever INTEGER NOT NULL,
	task INTEGER NOT NULL,
	prvid INTEGER NOT NULL,
	opid INTEGER NOT NULL,
	opdesc TEXT NULL,
	UNIQUE (eid, ever, task, opid, prvid)
);

CREATE TABLE EventXml (
	rid INTEGER PRIMARY KEY,
	xml TEXT NOT NULL
);

CREATE TABLE EventHeader (
	rid INTEGER PRIMARY KEY,
	stamp INTEGER NOT NULL,
	eid INTEGER NOT NULL,
	ever INTEGER NOT NULL,
	task INTEGER NOT NULL,
	prvid INTEGER NOT NULL,
	opid INTEGER NOT NULL
);
";

  }

}
