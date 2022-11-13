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
  /// Provides info on the content of a V2 database.
  /// Essentially an OperationInfoRow with additional information linked in
  /// </summary>
  public class DbOverviewRow: OperationInfoRow
  {
    /// <summary>
    /// Create a new DbOverviewRow
    /// </summary>
    public DbOverviewRow(
      long prvid,
      long eid,
      long ever,
      long task,
      long opid,
      string? prvname,
      string? taskdesc,
      string? opdesc,
      long eventcount)
      : base(eid, ever, task, prvid, opid, opdesc)
    {
      ProviderName = prvname;
      TaskDescription = taskdesc;
      EventCount = (int)eventcount;
    }
    /// <summary>
    /// Create a new DbOverviewRow with event count 0
    /// </summary>
    public DbOverviewRow(
      long prvid,
      long eid,
      long ever,
      long task,
      long opid,
      string? prvname,
      string? taskdesc,
      string? opdesc)
      : base(eid, ever, task, prvid, opid, opdesc)
    {
      ProviderName = prvname;
      TaskDescription = taskdesc;
      EventCount = 0;
    }

    /// <summary>
    /// The provider name (may be null in case of misconfiguration)
    /// </summary>
    public string? ProviderName { get; }

    /// <summary>
    /// The task description
    /// </summary>
    public string? TaskDescription { get; }

    /// <summary>
    /// The event count. Or 0L if not queried.
    /// </summary>
    public int EventCount { get; }

  }
}
