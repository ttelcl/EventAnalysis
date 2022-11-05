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
  /// A row in the EventXml table of the V2 database
  /// </summary>
  public class EventXmlRow: IEventKey
  {
    /// <summary>
    /// Create a new EventXmlRow
    /// </summary>
    public EventXmlRow(
      long rid,
      string xml)
    {
      RecordId = rid;
      Xml = xml;
    }

    /// <summary>
    /// The record ID
    /// </summary>
    public long RecordId { get; }

    /// <summary>
    /// The full event record in XML form. Note that the string may
    /// not be fully well-formed. In particular: disable the valid XML 
    /// character checking.
    /// </summary>
    public string Xml { get; }

  }
}
