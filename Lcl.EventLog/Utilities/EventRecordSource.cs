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

namespace Lcl.EventLog.Utilities
{
  /// <summary>
  /// Provides access to a sequence of event log records
  /// </summary>
  public class EventRecordSource
  {
    /// <summary>
    /// Create a new EventRecordSource.
    /// This also verifies it can be accessed; make sure to be ready for
    /// <see cref="EventLogNotFoundException"/> and
    /// <see cref="UnauthorizedAccessException"/>.
    /// </summary>
    /// <param name="eventLogName">
    /// The name of the event log to open.
    /// For a list of valid names, see <see cref="EventLogSession.GetLogNames"/>
    /// </param>
    /// <exception cref="EventLogNotFoundException">
    /// Thrown if the event log does not exist
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown if the calling user does not have enough privileges to access
    /// the log. Usually that means: admin elevation required.
    /// </exception>
    public EventRecordSource(
      string eventLogName)
    {
      EventLogName = eventLogName;
      var els = EventLogSession.GlobalSession;
      // The main purpose of the next line is to fail early in case
      // the log does not exist or requires admin permission
      Info = els.GetLogInformation(EventLogName, PathType.LogName);
    }

    /// <summary>
    /// The name of the event log
    /// </summary>
    public string EventLogName { get; }

    /// <summary>
    /// Information about the event log
    /// </summary>
    public EventLogInformation Info { get; }

    /// <summary>
    /// Enumerate all records in the log with IDs above the given record
    /// ID. Only records with a valid record id and timestamp are returned.
    /// </summary>
    public IEnumerable<EventLogRecord> ReadRecords(
      long? aboveRid)
    {
      var eventQuery =
        aboveRid.HasValue 
        ? $"*[System/EventRecordID > {aboveRid.Value}]"
        : "*";
      var elq = new EventLogQuery(EventLogName, PathType.LogName, eventQuery);
      using(var logReader = new EventLogReader(elq))
      {
        EventRecord e;
        while((e = logReader.ReadEvent())!=null)
        {
          if(e.RecordId.HasValue && e.TimeCreated.HasValue)
          {
            var elr = (EventLogRecord)e;
            yield return elr;
          }
        }
      }
    }

  }
}
