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
  /// An item holding enough information to look up an OperationInfoRow
  /// </summary>
  public interface IOperationInfoKey: ITaskInfoKey
  {
    /// <summary>
    /// The operation ID
    /// </summary>
    int OperationId { get; }
  }
}

