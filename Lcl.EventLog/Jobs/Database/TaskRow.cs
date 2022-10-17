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
  /// Models a row in the Tasks table
  /// </summary>
  public class TaskRow
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

  }
}
