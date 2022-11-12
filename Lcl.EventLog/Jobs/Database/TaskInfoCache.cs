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
  /// In-memory cache and new entry tracker for the TaskInfo table
  /// </summary>
  public class TaskInfoCache
  {
    private readonly BackedMap<TaskInfoRow, ValueTuple<int, int, int, int>> _map;

    /// <summary>
    /// Create a new TaskInfoCache
    /// </summary>
    public TaskInfoCache(
      IEnumerable<TaskInfoRow> existingRows)
    {
      var backing = new KeyedMap<TaskInfoRow, ValueTuple<int, int, int, int>>(existingRows);
      _map = new BackedMap<TaskInfoRow, (int, int, int, int)>(backing);
    }

    /// <summary>
    /// Create a new TaskInfoCache, loading its initial content from the given open database
    /// </summary>
    public static TaskInfoCache FromDb(OpenDbV2 odb)
    {
      return new TaskInfoCache(odb.AllTaskInfoRows());
    }

    /// <summary>
    /// Observe a new eventId-eventVersion-taskId-providerId quadruplet, inserting it 
    /// in this caches's front store if it is new or updated.
    /// </summary>
    public void Observe(int eid, int ever, int task, int prvid, string? description)
    {
      var old = _map.Find((eid, ever, task, prvid));
      if(old == null || (description!=null && old.TaskDescription!=description))
      {
        _map.Put(new TaskInfoRow(eid, ever, task, prvid, description ?? old?.TaskDescription));
      }
    }

    /// <summary>
    /// Observe an event header row, potentially creating a new task info row
    /// in this cache.
    /// </summary>
    /// <param name="ehr">
    /// The record containing the fields to observe
    /// </param>
    /// <param name="descriptionLoader">
    /// A function that loads the task description if needed. This is a function
    /// because that load can be an expensive operation.
    /// </param>
    public void Observe(EventHeaderRow ehr, Func<string?> descriptionLoader)
    {
      var old = Find(ehr);
      if(old == null || old.TaskDescription == null)
      {
        var description = descriptionLoader();
        if(old == null || (description!=null && old.TaskDescription!=description))
        {
          // This "if" does not trigger in case the old record existed and the loader returned null
          _map.Put(new TaskInfoRow(
            ehr.EventId, ehr.EventVersion, ehr.TaskId, ehr.ProviderId,
            description ?? old?.TaskDescription));
        }
      }
    }

    /// <summary>
    /// Find the existing row for the given eventId-eventVersion-taskId-providerId quadruplet,
    /// returning null if not found in the existing nor observed rows.
    /// </summary>
    public TaskInfoRow? Find(int eid, int ever, int task, int prvid)
    {
      return _map.Find((eid, ever, task, prvid));
    }

    /// <summary>
    /// Find the existing row matching the given event record header, returning null
    /// if not found
    /// </summary>
    public TaskInfoRow? Find(ITaskInfoKey itik)
    {
      return Find(itik.EventId, itik.EventVersion, itik.TaskId, itik.ProviderId);
    }

    /// <summary>
    /// Look up task description in this cache (if known)
    /// </summary>
    public string? TaskDescription(int eid, int ever, int task, int prvid)
    {
      return Find(eid, ever, task, prvid)?.TaskDescription;
    }

    /// <summary>
    /// Enumerate the newly observed items in this cache.
    /// </summary>
    public IEnumerable<TaskInfoRow> NewRows => _map.Items;

  }
}
