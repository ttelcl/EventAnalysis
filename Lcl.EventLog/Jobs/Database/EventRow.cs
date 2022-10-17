/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lcl.EventLog.Utilities;

namespace Lcl.EventLog.Jobs.Database
{
  /// <summary>
  /// Represents a row in the Events table
  /// </summary>
  public class EventRow
  {
    /// <summary>
    /// Create a new EventRow
    /// </summary>
    public EventRow(
      long rid,
      long eid,
      long task,
      long ts,
      long ver,
      string xml)
    {
      RecordId = rid;
      EventId = (int)eid;
      TaskId = (int)task;
      TickStamp = ts;
      TimeStamp = ts.EpochDateTime();
      Version = (int)ver;
      Xml = xml;
    }

    /// <summary>
    /// The record ID
    /// </summary>
    public long RecordId { get; }

    /// <summary>
    /// The event ID
    /// </summary>
    public int EventId { get; }

    /// <summary>
    /// The task ID
    /// </summary>
    public int TaskId { get; }

    /// <summary>
    /// The time stamp in ticks since the Epoch
    /// </summary>
    public long TickStamp { get; }

    /// <summary>
    /// The UTC time stamp
    /// </summary>
    public DateTime TimeStamp { get; }

    /// <summary>
    /// The event version
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// The XML representation of the full event
    /// </summary>
    public string Xml { get; }

  }
}
