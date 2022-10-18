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
  /// Models a row in the Tasks table
  /// </summary>
  public class TaskRow: IEquatable<TaskRow>, IHasKey<ValueTuple<int, int>>
  {
    /// <summary>
    /// Create a new TaskRow (typically invoked by Dapper)
    /// </summary>
    public TaskRow(long eid, long task, string? description)
    {
      EventId = (int)eid;
      TaskId = (int)task;
      Description = description;
    }

    /// <summary>
    /// The event ID
    /// </summary>
    public int EventId { get; }

    /// <summary>
    /// The task ID
    /// </summary>
    public int TaskId { get; }

    /// <summary>
    /// The description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Implements IEquatable
    /// </summary>
    public bool Equals(TaskRow? other)
    {
      return
        other != null
        && other.EventId == EventId
        && other.TaskId == TaskId
        && other.Description == Description;
    }

    /// <summary>
    /// Implements IHasKey
    /// </summary>
    public ValueTuple<int, int> Key => (EventId, TaskId);
  }
}
