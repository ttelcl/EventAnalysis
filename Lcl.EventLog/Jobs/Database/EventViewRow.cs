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
  /// A combination of an EventHeaderRow and an EventXmlRow
  /// </summary>
  public class EventViewRow: EventHeaderRow, IEventKey
  {
    /// <summary>
    /// Create a new EventViewRow
    /// </summary>
    public EventViewRow(
      long rid,
      long stamp,
      long eid,
      long ever,
      long task,
      long prvid,
      long opid,
      string xml)
      : base(rid, stamp, eid, ever, task, prvid, opid)
    {
      Xml = xml;
    }

    /// <summary>
    /// The raw event XML
    /// </summary>
    public string Xml { get; }

  }
}
