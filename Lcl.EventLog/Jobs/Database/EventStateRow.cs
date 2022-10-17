/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lcl.EventLog.Jobs.Database
{
  /// <summary>
  /// Models a row in the EventState table
  /// </summary>
  public class EventStateRow
  {
    /// <summary>
    /// Create a new EventStateRow
    /// </summary>
    public EventStateRow(long eid, long minversion = 0, long enabled = 1)
    {
      Eid = (int)eid;
      MinVersion = (int)minversion;
      Enabled = enabled != 0;
    }

    /// <summary>
    /// The event ID
    /// </summary>
    public int Eid { get; }

    /// <summary>
    /// The minimum event version to import
    /// </summary>
    public int MinVersion { get; set; }

    /// <summary>
    /// Whether or not to import this event type
    /// </summary>
    public bool Enabled { get; set; }

  }
}
