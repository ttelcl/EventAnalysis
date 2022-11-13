/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Lcl.EventLog.Utilities;

namespace Lcl.EventLog.Jobs.Database
{
  /// <summary>
  /// In-memory cache and new entry tracker for the OperationInfo table
  /// </summary>
  public class OperationInfoCache
  {
    private readonly BackedMap<OperationInfoRow, ValueTuple<int, int, int, int, int>> _map;

    /// <summary>
    /// Create a new OperationInfoCache
    /// </summary>
    public OperationInfoCache(
      IEnumerable<OperationInfoRow> existingRows)
    {
      var backing = new KeyedMap<OperationInfoRow, ValueTuple<int, int, int, int, int>>(existingRows);
      _map = new BackedMap<OperationInfoRow, (int, int, int, int, int)>(backing);
    }

    /// <summary>
    /// Create a new OperationInfoCache, loading its initial content from the given open database
    /// </summary>
    public static OperationInfoCache FromDb(OpenDbV2 odb)
    {
      return new OperationInfoCache(odb.AllOperationInfoRows());
    }

    /// <summary>
    /// Observe a new eventId-eventVersion-taskId-providerId-operationId quintuplet, inserting it 
    /// in this caches's front store if it is new or updated.
    /// </summary>
    public void Observe(int eid, int ever, int task, int prvid, int opid, string? description)
    {
      var old = _map.Find((eid, ever, task, prvid, opid));
      if(old == null || (description!=null && old.OperationDescription!=description))
      {
        _map.Put(new OperationInfoRow(eid, ever, task, prvid, opid, description ?? old?.OperationDescription));
      }
    }

    /// <summary>
    /// Observe an event header row, potentially creating a new operation info row
    /// in this cache.
    /// </summary>
    /// <param name="ehr">
    /// The record containing the fields to observe
    /// </param>
    /// <param name="descriptionLoader">
    /// A function that loads the operation description if needed. This is a function
    /// because that load can be an expensive operation.
    /// </param>
    public void Observe(EventHeaderRow ehr, Func<string?> descriptionLoader)
    {
      var old = Find(ehr);
      if(old == null || old.OperationDescription == null)
      {
        var description = descriptionLoader();
        if(old == null || (description!=null && old.OperationDescription!=description))
        {
          // This "if" does not trigger in case the old record existed and the loader returned null
          _map.Put(new OperationInfoRow(
            ehr.EventId, ehr.EventVersion, ehr.TaskId, ehr.ProviderId, ehr.OperationId,
            description ?? old?.OperationDescription));
        }
      }
    }

    /// <summary>
    /// Find the existing row for the given eventId-eventVersion-taskId-providerId quadruplet,
    /// returning null if not found in the existing nor observed rows.
    /// </summary>
    public OperationInfoRow? Find(int eid, int ever, int task, int prvid, int opid)
    {
      return _map.Find((eid, ever, task, prvid, opid));
    }

    /// <summary>
    /// Find the existing row matching the given event record header, returning null
    /// if not found
    /// </summary>
    public OperationInfoRow? Find(IOperationInfoKey ioik)
    {
      return Find(ioik.EventId, ioik.EventVersion, ioik.TaskId, ioik.ProviderId, ioik.OperationId);
    }

    /// <summary>
    /// Look up task description in this cache (if known)
    /// </summary>
    public string? TaskDescription(int eid, int ever, int task, int prvid, int opid)
    {
      return Find(eid, ever, task, prvid, opid)?.OperationDescription;
    }

    /// <summary>
    /// Enumerate the newly observed items in this cache.
    /// </summary>
    public IEnumerable<OperationInfoRow> NewRows => _map.Items;

  }
}
