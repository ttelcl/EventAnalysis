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
  /// Ways to handle table key conflicts on insertions
  /// </summary>
  public enum ConflictMode
  {
    /// <summary>
    /// Use the default handling (normally: throw an exception)
    /// </summary>
    Default = 0,

    /// <summary>
    /// Replace the record
    /// </summary>
    Replace = 1,

    /// <summary>
    /// Ignore the new value and leave the old one in place
    /// </summary>
    Ignore = 2,
  }
}
