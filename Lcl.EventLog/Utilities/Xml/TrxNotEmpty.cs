/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lcl.EventLog.Utilities.Xml
{
  /// <summary>
  /// A validation string transform that ensures the string
  /// to transform is not null nor empty
  /// </summary>
  public class TrxNotEmpty: XmlFieldTransform
  {
    /// <summary>
    /// Create a new TrxNotEmpty
    /// </summary>
    public TrxNotEmpty() : base("notempty")
    {
    }

    /// <summary>
    /// Implements the transform
    /// </summary>
    public override string Transform(string value, XmlEventQuery caller)
    {
      if(String.IsNullOrEmpty(value))
      {
        throw new InvalidOperationException(
          $"Expecting a non-empty string for field '{caller.Label}'");
      }
      return value;
    }
  }
}