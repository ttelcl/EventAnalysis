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
  /// Tracks information on events that are about to be inserted
  /// </summary>
  public class EventInfoTracker
  {
    /// <summary>
    /// Create a new EventInfoTracker
    /// </summary>
    public EventInfoTracker(
      IEnumerable<EventStateRow> existingStates,
      IEnumerable<TaskRow> existingTasks)
    {
      StateTracker = new EventStateTracker(existingStates);
      TaskTracker = new TaskTracker(existingTasks);
    }

    /// <summary>
    /// The tracker for discovering new EventState entries
    /// </summary>
    public EventStateTracker StateTracker { get; }

    /// <summary>
    /// The tracker for discovering new or modified Tasks entries
    /// </summary>
    public TaskTracker TaskTracker { get; }

    /// <summary>
    /// Whether or not to import an event record with the given event id and version.
    /// Note that the decision whether or not to track the event is independent.
    /// </summary>
    public bool ShouldProcess(int eventId, int version)
    {
      var eventState = StateTracker[eventId];
      if(eventState != null)
      {
        return eventState.Enabled && version >= eventState.MinVersion;
      }
      else
      {
        return true;
      }
    }

    /// <summary>
    /// Observe an event: update the embedded trackers
    /// </summary>
    public void ObserveEvent(int eventId, int taskId, string? description)
    {
      StateTracker.PutObservation(eventId);
      TaskTracker.PutObservation(eventId, taskId, description);
    }

    /// <summary>
    /// The event state records that were added or modified (need to be inserted in the DB)
    /// </summary>
    public IEnumerable<EventStateRow> UpdatedStates => StateTracker.Items;

    /// <summary>
    /// The task records that were added or modified (need to be inserted in the DB)
    /// </summary>
    public IEnumerable<TaskRow> UpdatedTasks => TaskTracker.Items;
  }
}
