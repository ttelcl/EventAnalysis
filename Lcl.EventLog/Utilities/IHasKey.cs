/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lcl.EventLog.Utilities
{
  /// <summary>
  /// An object that has an identifying key
  /// </summary>
  public interface IHasKey<TKey>
    where TKey: IEquatable<TKey>
  {
    /// <summary>
    /// The key identifying the object
    /// </summary>
    TKey Key { get; }
  }
}

