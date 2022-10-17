﻿/*
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
    public RawEventDb(string fileName, bool allowWrite, bool allowCreate=false)
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

      private SqliteConnection Connection { get; }

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
", new { Eid = eid, MinVersion = minversion, Enabled = enabled ? 1 : 0});
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
", new {Eid = eid, Task = task, Description = description});
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
