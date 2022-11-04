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
  /// Description of TaskInfoRow
  /// </summary>
  public class TaskInfoRow: IHasKey<ValueTuple<int, int, int, int>>
  {
    /// <summary>
    /// Create a new TaskInfoRow
    /// </summary>
    public TaskInfoRow(
      int eid,
      int ever,
      int task,
      int prvid,
      string? taskdesc)
    {
      EventId=eid;
      EventVersion=ever;
      TaskId=task;
      ProviderId=prvid;
      TaskDescription=taskdesc;
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
    /// The task description. Null indicates that no attempt has been made to
    /// discover it yet. An empty string may mean that an attempt was made
    /// but failed.
    /// </summary>
    public string? TaskDescription { get; }

    /// <inheritdoc/>
    public (int, int, int, int) Key => (EventId, EventVersion, TaskId, ProviderId);
  }
}
