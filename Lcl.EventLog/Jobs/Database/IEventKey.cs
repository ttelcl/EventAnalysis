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
  /// An item holding enough information to look up an EventHeaderRow or EventXmlRow
  /// </summary>
  public interface IEventKey
  {
    /// <summary>
    /// The record ID
    /// </summary>
    long RecordId { get; }
  }
}

