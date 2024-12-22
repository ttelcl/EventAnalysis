/*
 * (c) 2024  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lcl.EventLog.Jobs.Database;

/// <summary>
/// Tracks statistics for a group of event rows
/// </summary>
public class EventRowStatistics
{
  /// <summary>
  /// Create a new EventRowStatistics
  /// </summary>
  public EventRowStatistics()
  {
    Reset("");
  }

  /// <summary>
  /// A tag string. Not used internally, but clients can use it
  /// to determine if a row belongs to the current group.
  /// Changed by calling <see cref="Reset(string)"/>
  /// </summary>
  public string Tag { get; private set; } = "";

  /// <summary>
  /// The number of rows in the group
  /// </summary>
  public int Count { get; set; } = 0;

  /// <summary>
  /// The minimum record ID in the group
  /// </summary>
  public long MinRid { get; set; } = long.MaxValue;

  /// <summary>
  /// The maximum record ID in the group
  /// </summary>
  public long MaxRid { get; set; } = long.MinValue;

  /// <summary>
  /// The total size of the XML content of the records in the group
  /// </summary>
  public int TotalSize { get; set; } = 0;

  /// <summary>
  /// The size of the largest record in the group
  /// </summary>
  public int MaxSize { get; set; } = 0;

  /// <summary>
  /// Minimum timestamp in the group (in epoch ticks)
  /// </summary>
  public long StampMin { get; set; } = long.MaxValue;

  /// <summary>
  /// Maximum timestamp in the group (in epoch ticks)
  /// </summary>
  public long StampMax { get; set; } = long.MinValue;

  /// <summary>
  /// Reset the statistics and associate a new tag
  /// </summary>
  public void Reset(string tag)
  {
    Tag = tag;
    Count = 0;
    MinRid = long.MaxValue;
    MaxRid = long.MinValue;
    TotalSize = 0;
    StampMin = long.MaxValue;
    StampMax = long.MinValue;
    MaxSize = 0;
  }

  /// <summary>
  /// Observe a row, updating the statistics
  /// </summary>
  public void ObserveRow(EventViewRow row)
  {
    var length = row.Xml.Length;
    Count++;
    MinRid = Math.Min(MinRid, row.RecordId);
    MaxRid = Math.Max(MaxRid, row.RecordId);
    TotalSize += length;
    StampMin = Math.Min(StampMin, row.Stamp);
    StampMax = Math.Max(StampMax, row.Stamp);
    MaxSize = Math.Max(MaxSize, length);
  }

}
