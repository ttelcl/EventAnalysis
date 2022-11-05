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
  /// Tracks new TaskRows
  /// </summary>
  public class TaskTracker
  {
    private readonly BackedMap<TaskRow, ValueTuple<int,int>> _map;

    /// <summary>
    /// Create a new TaskTracker
    /// </summary>
    public TaskTracker(
      IEnumerable<TaskRow> existingRows)
    {
      var backing = new KeyedMap<TaskRow, ValueTuple<int, int>>(existingRows);
      _map = new BackedMap<TaskRow, ValueTuple<int, int>>(backing);
    }

    /// <summary>
    /// Register a new observation of the (event,task) pair. The registration
    /// is skipped if it already exists and this new registration would not
    /// update it.
    /// </summary>
    public void PutObservation(int eid, int task, string? description = null)
    {
      var old = _map.Find((eid, task));
      if(old == null || (description!=null && old.Description!=description))
      {
        _map.Put(new TaskRow(eid, task, description ?? old?.Description));
      }
    }

    /// <summary>
    /// Find a task row matching the key in this map or the backing
    /// </summary>
    public TaskRow? this[int eid, int task] {
      get => _map.Find((eid, task));
    }

    /// <summary>
    /// Return the description for the given event-task pair, returning
    /// null if not known
    /// </summary>
    public string? Description(int eid, int task)
    {
      return this[eid, task]?.Description;
    }

    /// <summary>
    /// Get the rows newly added to this TaskTracker via PutObservation()
    /// </summary>
    public IEnumerable<TaskRow> Items => _map.Items;
  }
}
