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

namespace Lcl.EventLog.Utilities
{
  /// <summary>
  /// Static methods for converting string values from XML records
  /// into other common types
  /// </summary>
  public static class XmlStringValueConversions
  {
    /// <summary>
    /// Parse a string as an integer, supporting three cases:
    /// Null or an empty string results in null; A string starting
    /// with 0x or 0X is interpreted as hexadecimal; Other strings
    /// are interpreted as decimal integers
    /// </summary>
    public static long? AsInteger(this string txt)
    {
      if(String.IsNullOrEmpty(txt))
      {
        return null;
      }
      if(txt.StartsWith("0x") || txt.StartsWith("0X"))
      {
        txt = txt.Substring(2);
        return Int64.Parse(txt, NumberStyles.AllowHexSpecifier);
      }
      return Int64.Parse(txt, NumberStyles.None);
    }

    /// <summary>
    /// Parse a string as an unsigned integer, supporting three cases:
    /// Null or an empty string results in null; A string starting
    /// with 0x or 0X is interpreted as hexadecimal; Other strings
    /// are interpreted as decimal integers
    /// </summary>
    public static ulong? AsUnsigned(this string txt)
    {
      if(String.IsNullOrEmpty(txt))
      {
        return null;
      }
      if(txt.StartsWith("0x") || txt.StartsWith("0X"))
      {
        txt = txt.Substring(2);
        return UInt64.Parse(txt, NumberStyles.AllowHexSpecifier);
      }
      return UInt64.Parse(txt, NumberStyles.None);
    }

    /// <summary>
    /// Parse a string as null or Guid: an empty or null string 
    /// returns null; other strings are parsed as GUID
    /// </summary>
    public static Guid? AsGuid(this string txt)
    {
      return String.IsNullOrEmpty(txt)
        ? null
        : Guid.Parse(txt);
    }

    /// <summary>
    /// Parse a string as null or roundtrip DateTimeOffset: an empty or null string 
    /// returns null; other strings are parsed as DateTimeOffset in RoundTrip format
    /// </summary>
    public static DateTimeOffset? AsStamp(this string txt)
    {
      return String.IsNullOrEmpty(txt)
        ? null
        : DateTimeOffset.Parse(txt, null, DateTimeStyles.RoundtripKind);
    }

  }
}