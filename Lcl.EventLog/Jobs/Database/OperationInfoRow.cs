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
  /// Extra information on an Operation (in the context of a given
  /// EventId, EventVersion, TaskId, Provider)
  /// </summary>
  public class OperationInfoRow: 
    IHasKey<ValueTuple<int, int, int, int, int>>, 
    IOperationInfoKey, ITaskInfoKey, IProviderInfoKey
  {
    /// <summary>
    /// Create a new OperationInfoRow
    /// </summary>
    public OperationInfoRow(
      long eid,
      long ever,
      long task,
      long prvid,
      long opid,
      string? opdesc)
    {
      EventId = (int)eid;
      EventVersion = (int)ever;
      TaskId = (int)task;
      ProviderId = (int)prvid;
      OperationId = (int)opid;
      OperationDescription=opdesc;
    }

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

    /// <summary>
    /// The task description. Null indicates that no attempt has been made to
    /// discover it yet. An empty string may mean that an attempt was made
    /// but failed.
    /// </summary>
    public string? OperationDescription { get; }

    /// <inheritdoc/>
    public (int, int, int, int, int) Key => (EventId, EventVersion, TaskId, ProviderId, OperationId);

  }
}
