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
  /// An item holding enough information to look up a ProviderInfoRow
  /// </summary>
  public interface IProviderInfoKey
  {
    /// <summary>
    /// The provider ID (referencing a ProviderInfoRow)
    /// </summary>
    int ProviderId { get; }
  }
}

