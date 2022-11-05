/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lcl.EventLog.Jobs.Database
{
  /// <summary>
  /// An item holding enough information to look up a TaskInfoRow
  /// </summary>
  public interface ITaskInfoKey: IProviderInfoKey
  {
    /// <summary>
    /// The event ID
    /// </summary>
    int EventId { get; }

    /// <summary>
    /// The event version
    /// </summary>
    int EventVersion { get; }

    /// <summary>
    /// The task ID
    /// </summary>
    int TaskId { get; }
  }
}

