/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dapper;

using Microsoft.Data.Sqlite;

namespace Lcl.EventLog.Jobs.Database
{

  /// <summary>
  /// Represents an Open Sqlite connection
  /// </summary>
  public class OpenDbV2: IDisposable
  {
    internal OpenDbV2(SqliteConnection conn, bool canWrite, bool canCreate)
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
    /// Insert a new ProviderInfo record. This operation fails
    /// if the ProviderId or ProviderName already exists
    /// </summary>
    public int InsertProviderInfo(int prvId, string prvName, string? prvGuid)
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
    /// Return all provider info records 
    /// </summary>
    public IEnumerable<ProviderInfoRow> ReadProviderInfo()
    {
      return Connection.Query<ProviderInfoRow>(@"
SELECT prvid, prvname, prvguid
FROM ProviderInfo");
    }

    private void InitTables()
    {
      if(!CanWrite || !CanCreate)
      {
        throw new InvalidOperationException(
          "Cannot create tables. The database connection is in no-create mode.");
      }
      Connection.Execute(__dbCreateSql);
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
