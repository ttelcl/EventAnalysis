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

using Lcl.EventLog.Utilities;

using Microsoft.Data.Sqlite;

namespace Lcl.EventLog.Jobs.Database
{
  /// <summary>
  /// Imports a series of events into a DB
  /// </summary>
  public class EventImportJob: IDisposable
  {
    private readonly RawEventDb.OpenDb _db;
    private readonly SqliteTransaction _trx;
    private bool _disposed;

    /// <summary>
    /// Create a new EventImportJob
    /// </summary>
    public EventImportJob(
      RawEventDb.OpenDb db)
    {
      _db = db;
      _trx = _db.Connection.BeginTransaction();
      var tasks = db.ReadTasks().ToList();
      var states = db.ReadEventStates().ToList();
      Tracker = new EventInfoTracker(states, tasks);
    }

    /// <summary>
    /// The tracker object that keeps track of new and modified tasks and event states.
    /// </summary>
    public EventInfoTracker Tracker { get; }

    /// <summary>
    /// Whether or not to overwrite events. Initially set to ConflictMode.Default.
    /// </summary>
    public ConflictMode ConflictHandling { get; set; } = ConflictMode.Default;

    /// <summary>
    /// Process an event from an Event Log.
    /// </summary>
    /// <param name="elr">
    /// The event record
    /// </param>
    /// <returns>
    /// True if the event was imported, false if it wasn't processed due
    /// to not meeting the criteria set in the EventState table. Even if not
    /// imported, it is still tracked.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The event record is missing its RecordId or TimeCreated fields
    /// </exception>
    public bool ProcessEvent(EventLogRecord elr)
    {
      ThrowIfDisposed();
      var eventId = elr.Id;
      if(!elr.RecordId.HasValue)
      {
        throw new ArgumentException(
          $"The event record has no record ID");
      }
      if(!elr.TimeCreated.HasValue)
      {
        throw new ArgumentException(
          $"The event record has no time stamp");
      }
      var recordId = elr.RecordId.Value;
      var taskId = elr.Task ?? 0;
      string? taskDescription = null;
      try
      {
        taskDescription = elr.TaskDisplayName;
      }
      catch(Exception ex)
      {
        Trace.TraceInformation("Ignoring metadata lookup exception: " + ex.Message);
      }
      int eventVersion = elr.Version ?? 0;
      var stamp = elr.TimeCreated.Value;
      // observe even ignored events!
      Tracker.ObserveEvent(eventId, taskId, taskDescription);
      if(Tracker.ShouldProcess(eventId, eventVersion))
      {
        var xml = elr.ToXml();
        // xml = XmlUtilities.FixXml(xml);
        var n = _db.PutEvent(recordId, eventId, taskId, stamp, eventVersion, xml, ConflictHandling);
        return n > 0;
      }
      else
      {
        return false;
      }
    }

    /// <summary>
    /// Process a fully specified event record (using a time stamp specified as DateTime)
    /// </summary>
    /// <returns>
    /// True if the event was inserted, false if it didn't match the event filter
    /// </returns>
    public bool ProcessEvent(
      long recordId,
      int eventId,
      int taskId,
      DateTime utcStamp,
      int eventVersion,
      string xml,
      string? taskDescription = null)
    {
      ThrowIfDisposed();
      // observe even ignored events!
      Tracker.ObserveEvent(eventId, taskId, taskDescription);
      if(Tracker.ShouldProcess(eventId, eventVersion))
      {
        var n = _db.PutEvent(recordId, eventId, taskId, utcStamp, eventVersion, xml, ConflictHandling);
        return n > 0;
      }
      else
      {
        return false;
      }
    }

    /// <summary>
    /// Process a fully specified event record (using a time stamp specified as ticks since Epoch)
    /// </summary>
    /// <returns>
    /// True if the event was inserted, false if it didn't match the event filter
    /// </returns>
    public bool ProcessEvent(
      long recordId,
      int eventId,
      int taskId,
      long epochTicks,
      int eventVersion,
      string xml,
      string? taskDescription = null)
    {
      ThrowIfDisposed();
      // observe even ignored events!
      Tracker.ObserveEvent(eventId, taskId, taskDescription);
      if(Tracker.ShouldProcess(eventId, eventVersion))
      {
        var n = _db.PutEvent(recordId, eventId, taskId, epochTicks, eventVersion, xml, ConflictHandling);
        return n > 0;
      }
      else
      {
        return false;
      }
    }

    /// <summary>
    /// Process an event specified as XML. Note that task descriptions are not available
    /// in this form
    /// </summary>
    /// <param name="xml">
    /// The XML formatted event record
    /// </param>
    /// <returns>
    /// True if inserted, false if rejected by the event filter
    /// </returns>
    /// <exception cref="NotImplementedException"></exception>
    public bool ProcessEvent(string xml)
    {
      ThrowIfDisposed();
      throw new NotImplementedException(
        "Not yet implemented");
    }

    /// <summary>
    /// Commit this job's transaction.
    /// Also disposes this job, preventing most other interactions.
    /// </summary>
    public void Commit(bool emitTracker)
    {
      ThrowIfDisposed();
      if(emitTracker)
      {
        foreach(var task in Tracker.UpdatedTasks)
        {
          _db.PutTask(task.EventId, task.TaskId, task.Description);
        }
        foreach(var state in Tracker.UpdatedStates)
        {
          _db.PutEventState(state.Eid, state.MinVersion, state.Enabled);
        }
      }
      _trx.Commit();
      Dispose();
    }

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
