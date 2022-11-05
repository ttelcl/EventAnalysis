/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Data.Sqlite;

using Lcl.EventLog.Utilities;
using Lcl.EventLog.Utilities.Xml;

namespace Lcl.EventLog.Jobs.Database
{
  /// <summary>
  /// Event import functionality for the V2 database model
  /// </summary>
  public class EventImportJob2: IDisposable
  {
    private readonly OpenDbV2 _db;
    private readonly SqliteTransaction _trx;
    private bool _disposed;

    /// <summary>
    /// Create a new EventImportJob2
    /// </summary>
    public EventImportJob2(OpenDbV2 db)
    {
      _db = db;
      _trx = _db.Connection.BeginTransaction();
      Tracker = new EventInsertionLogic(
        _db.AllProviderInfoRows().ToList(),
        _db.AllTaskInfoRows().ToList(),
        _db.AllOperationInfoRows().ToList());
    }

    /// <summary>
    /// Inserts the newly added Provider, task and operation records and
    /// commits this job's transaction.
    /// Also disposes this job, preventing most other interactions.
    /// </summary>
    public void Commit()
    {
      ThrowIfDisposed();
      foreach(var pir in Tracker.ProviderInfoTracker.NewlyAdded)
      {
        _db.InsertProviderInfoRow(pir);
      }
      foreach(var tir in Tracker.TaskInfoTracker.NewRows)
      {
        _db.InsertTaskInfoRow(tir);
      }
      foreach(var oir in Tracker.OperationInfoTracker.NewRows)
      {
        _db.InsertOperationInfoRow(oir);
      }
      _trx.Commit();
      Dispose();
    }

    /// <summary>
    /// Insert an event into the database from an EventLogRecord
    /// </summary>
    public bool ProcessEvent(EventLogRecord elr)
    {
      if(elr.RecordId.HasValue && elr.TimeCreated.HasValue)
      {
        var xml = elr.ToXml();
        if(!String.IsNullOrEmpty(xml))
        {
          var ehr = Tracker.MakeHeaderFromEventLogRecord(elr);
          _db.InsertEvent(ehr, xml);
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Insert an event into the database from an XML representation
    /// </summary>
    public bool ProcessEvent(string xml)
    {
      if(!String.IsNullOrEmpty(xml))
      {
        var dissector = new XmlEventDissector(xml);
        var ehr = Tracker.MakeHeaderFromXml(dissector);
        _db.InsertEvent(ehr, xml);
        return true;
      }
      return false;
    }

    /// <summary>
    /// The event converter and provider / task / operation tracker
    /// </summary>
    public EventInsertionLogic Tracker { get; }

    /// <summary>
    /// Clean up and finish the transaction. If not committed yet, this aborts the transaction.
    /// </summary>
    public void Dispose()
    {
      if(!_disposed)
      {
        _disposed = true;
        _trx.Dispose();
      }
    }

    private void ThrowIfDisposed()
    {
      if(_disposed)
      {
        throw new ObjectDisposedException(
          "EventImportJob");
      }
    }

  }
}
