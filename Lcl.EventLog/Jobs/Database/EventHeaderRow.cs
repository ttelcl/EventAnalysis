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
  /// A row in the EventHeader table of the V2 database
  /// </summary>
  public class EventHeaderRow
  {
    /// <summary>
    /// Create a new EventHeaderRow
    /// </summary>
    public EventHeaderRow(
      long rid,
      long stamp,
      long eid,
      long ever,
      long task,
      long prvid,
      long opid)
    {
      RecordId = rid;
      Stamp = stamp;
      EventId = (int)eid;
      EventVersion = (int)ever;
      TaskId = (int)task;
      ProviderId = (int)prvid;
      OperationId = (int)opid;
    }

    /// <summary>
    /// The event record ID
    /// </summary>
    public long RecordId { get; }

    /// <summary>
    /// Timestamp in ticks since the Epoch
    /// </summary>
    public long Stamp { get; }

    /// <summary>
    /// The event ID
    /// </summary>
    public int EventId { get; }

    /// <summary>
    /// The event version
    /// </summary>
    public int EventVersion { get; }

    /// <summary>
    /// The task ID
    /// </summary>
    public int TaskId { get; }

    /// <summary>
    /// The provider ID (referencing a ProviderInfoRow)
    /// </summary>
    public int ProviderId { get; }

    /// <summary>
    /// The operation ID
    /// </summary>
    public int OperationId { get; }

  }
}
