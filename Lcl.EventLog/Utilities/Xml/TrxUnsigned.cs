/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lcl.EventLog.Utilities.Xml
{
  /// <summary>
  /// A string transform that ensures the value is a valid
  /// unsigned decimal integer or empty, transforming hexadecimal to
  /// decimal if needed.
  /// </summary>
  public class TrxUnsigned: XmlFieldTransform
  {
    /// <summary>
    /// Create a new TrxDecimal
    /// </summary>
    public TrxUnsigned() : base("unsigned")
    {
    }

    /// <summary>
    /// Implements the transform
    /// </summary>
    public override string Transform(string value, XmlEventQuery caller)
    {
      if(String.IsNullOrEmpty(value))
      {
        return value;
      }
      if(value.StartsWith("0x") || value.StartsWith("0X"))
      {
        value = value.Substring(2);
        if(UInt64.TryParse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var num))
        {
          return num.ToString();
        }
        else
        {
          throw new InvalidOperationException(
            $"Invalid hexadecimal representation for field '{caller.Label}': '{value}'");
        }
      }
      if(UInt64.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var num2))
      {
        return num2.ToString();
      }
      else
      {
        throw new InvalidOperationException(
          $"Invalid decimal representation for field '{caller.Label}': '{value}'");
      }
    }
  }
}
