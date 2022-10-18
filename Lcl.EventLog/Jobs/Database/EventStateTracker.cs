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
  /// Tracks new EventStateRows
  /// </summary>
  public class EventStateTracker
  {
    private readonly BackedMap<EventStateRow, int> _map;

    /// <summary>
    /// Create a new EventStateTracker
    /// </summary>
    public EventStateTracker(
      IEnumerable<EventStateRow> existingRows)
    {
      var backing = new KeyedMap<EventStateRow, int>(existingRows);
      _map = new BackedMap<EventStateRow, int>(backing);
    }

    /// <summary>
    /// Register an observation of the given event ID. If not present
    /// in this map or the backing map, store a new EventStateRow with
    /// the given event id and default values for other parameters
    /// </summary>
    public void PutObservation(int eid)
    {
      if(_map[eid] == null)
      {
        _map.Put(new EventStateRow(eid), false);
      }
    }

    /// <summary>
    /// Find the EventStateRow for the given Event ID in this map
    /// or its backing, returning null if not found.
    /// </summary>
    public EventStateRow? this[int eid] {
      get { return _map[eid]; }
    }

    /// <summary>
    /// Return a flag indicating if the event id should be considered
    /// enabled. The event counts as enabled if it is known and its
    /// Enabled flag is true, or if it is unknown.
    /// </summary>
    public bool IsEnabled(int eid)
    {
      return this[eid]?.Enabled ?? true;
    }

    /// <summary>
    /// Enumerate the rows that were newly added to this tracker using
    /// PutObservation().
    /// </summary>
    public IEnumerable<EventStateRow> Items => _map.Items;
  }
}
