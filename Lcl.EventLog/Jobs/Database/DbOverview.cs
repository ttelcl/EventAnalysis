/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Lcl.EventLog.Utilities;

namespace Lcl.EventLog.Jobs.Database
{
  /// <summary>
  /// Overview information on one eventId / taskId combination in the db
  /// </summary>
  public class DbOverview
  {
    /// <summary>
    /// Create the DbOverview instance
    /// </summary>
    public DbOverview(
      long eventId,
      long taskId,
      string? taskLabel,
      long minVersion,
      long isEnabled,
      long? eventCount,
      long? minRid,
      long? maxRid,
      long? eticksMin,
      long? eticksMax,
      long? xmlSize)
    {
      EventId = (int)eventId;
      TaskId = (int)taskId;
      TaskLabel = taskLabel;
      MinVersion = (int)minVersion;
      IsEnabled = isEnabled!=0L;
      EventCount = (int)(eventCount ?? 0L);
      MinRid = minRid ?? 0L;
      MaxRid = maxRid ?? 0L;
      UtcMin = eticksMin?.EpochDateTime();
      UtcMax = eticksMax?.EpochDateTime();
      XmlSize = xmlSize ?? 0L;
    }

    /// <summary>
    /// The event ID described in this row
    /// </summary>
    public int EventId { get; }

    /// <summary>
    /// The task ID described in this row
    /// </summary>
    public int TaskId { get; }

    /// <summary>
    /// The task description for the event+task described in this row.
    /// Null if not available
    /// </summary>
    public string? TaskLabel { get; }

    /// <summary>
    /// The lowest version of the event ID configured to be imported
    /// </summary>
    public int MinVersion { get; }

    /// <summary>
    /// Whether this event should be stored in this DB upon import
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    /// The number of events for this event+task combination
    /// </summary>
    public int EventCount { get; }

    /// <summary>
    /// The lowest event record ID for this event+task combination,
    /// or 0 if there are no such events
    /// </summary>
    public long MinRid { get; }

    /// <summary>
    /// The highest event record ID for this event+task combination,
    /// or 0 if there are no such events
    /// </summary>
    public long MaxRid { get; }

    /// <summary>
    /// The earliest timestamp for this event+task combination
    /// </summary>
    public DateTime? UtcMin { get; }

    /// <summary>
    /// The latest timestamp for this event+task combination
    /// </summary>
    public DateTime? UtcMax { get; }

    /// <summary>
    /// The total length of all XML strings in the database
    /// </summary>
    public long XmlSize { get; }
  }
}
