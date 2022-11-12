/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lcl.EventLog.Utilities;
using Lcl.EventLog.Utilities.Xml;

namespace Lcl.EventLog.Jobs.Database
{
  /// <summary>
  /// Part of the logic of inserting new event records in the V2 database,
  /// creating the actual header records to insert and tracking the missing
  /// ProviderInfo, TaskInfo, and OperationInfo records for later insertion
  /// </summary>
  public class EventInsertionLogic
  {
    /// <summary>
    /// Create a new EventInsertionLogic
    /// </summary>
    public EventInsertionLogic(
      IEnumerable<ProviderInfoRow> existingProviderInfo,
      IEnumerable<TaskInfoRow> existingTaskInfo,
      IEnumerable<OperationInfoRow> existingOperationInfo)
    {
      ProviderInfoTracker = new ProviderInfoCache(existingProviderInfo);
      TaskInfoTracker = new TaskInfoCache(existingTaskInfo);
      OperationInfoTracker = new OperationInfoCache(existingOperationInfo);
    }

    /// <summary>
    /// Create a new EventInsertionLogic, loading the initial state from the given open DB
    /// </summary>
    public EventInsertionLogic(
      OpenDbV2 odb)
    {
      ProviderInfoTracker = ProviderInfoCache.FromDb(odb);
      TaskInfoTracker = TaskInfoCache.FromDb(odb);
      OperationInfoTracker = OperationInfoCache.FromDb(odb);
    }

    /// <summary>
    /// Tracks newly added ProviderInfo rows (and assigns new providr IDs)
    /// </summary>
    public ProviderInfoCache ProviderInfoTracker { get; }

    /// <summary>
    /// Tracks newly added TaskInfo rows
    /// </summary>
    public TaskInfoCache TaskInfoTracker { get; }

    /// <summary>
    /// Tracks newly added OperationInfo rows
    /// </summary>
    public OperationInfoCache OperationInfoTracker { get; }

    /// <summary>
    /// Prepare the record for insertion in the DB, returning a new
    /// EventHeaderRow, and tracking it in <see cref="ProviderInfoTracker"/>,
    /// <see cref="TaskInfoTracker"/> and <see cref="OperationInfoTracker"/>.
    /// </summary>
    public EventHeaderRow MakeHeaderFromEventLogRecord(EventLogRecord elr)
    {
      var prvname = elr.ProviderName;
      var pir = ProviderInfoTracker.Find(prvname);
      if(pir == null)
      {
        ProviderInfoTracker.Observe(prvname, elr.ProviderId?.ToString("B"));
        pir = ProviderInfoTracker.Find(prvname);
      }
      var prvid = pir!.ProviderId;
      var ehr = new EventHeaderRow(
        elr.RecordId!.Value,
        TimeUtil.TicksSinceEpoch(elr.TimeCreated!.Value),
        elr.Id,
        elr.Version ?? 0,
        elr.Task ?? 0,
        prvid,
        elr.Opcode ?? 0);
      TaskInfoTracker.Observe(ehr, () => ExtractTaskDescription(elr));
      OperationInfoTracker.Observe(ehr, () => ExtractOperationDescription(elr));
      return ehr;
    }

    /// <summary>
    /// Prepare the record for insertion in the DB, returning a new
    /// EventHeaderRow, and tracking it in <see cref="ProviderInfoTracker"/>,
    /// <see cref="TaskInfoTracker"/> and <see cref="OperationInfoTracker"/>.
    /// This overload only needs an XML input, but cannot record task
    /// or operation descriptions.
    /// </summary>
    public EventHeaderRow MakeHeaderFromXml(XmlEventDissector xed)
    {
      var prvname = xed.Provider;
      var pir = ProviderInfoTracker.Find(prvname);
      if(pir == null)
      {
        ProviderInfoTracker.Observe(prvname, xed.ProviderGuid?.ToString("B"));
        pir = ProviderInfoTracker.Find(prvname);
      }
      var prvid = pir!.ProviderId;
      var ehr = new EventHeaderRow(
        xed.RecordId,
        TimeUtil.TicksSinceEpoch(xed.Stamp),
        xed.EventId,
        xed.Version,
        xed.TaskId,
        prvid,
        xed.Opcode);
      TaskInfoTracker.Observe(ehr, () => null);
      OperationInfoTracker.Observe(ehr, () => null);
      return ehr;
    }

    private static string ExtractTaskDescription(EventLogRecord elr)
    {
      try
      {
        return elr.TaskDisplayName;
      }
      catch(Exception)
      {
        // Sometimes evaluating elr.TaskDisplayName throws an exception because
        // the required metadata file no longer exists. There is nothing we can do
        // to avoid or fix it.
        // I hate to use a catch-all, but I don't see an alternative.
        return "";
      }
    }

    private static string ExtractOperationDescription(EventLogRecord elr)
    {
      try
      {
        return elr.OpcodeDisplayName;
      }
      catch(Exception)
      {
        // Sometimes evaluating elr.OpcodeDisplayName throws an exception because
        // the required metadata file no longer exists. There is nothing we can do
        // to avoid or fix it.
        // I hate to use a catch-all, but I don't see an alternative.
        return "";
      }
    }

  }
}
