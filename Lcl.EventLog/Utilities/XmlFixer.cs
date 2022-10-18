/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lcl.EventLog.Utilities
{
  /// <summary>
  /// Utility to fix some conformance errors in the XML generated
  /// by EventLogRecord.ToXml().
  /// </summary>
  public static class XmlFixer
  {
    /// <summary>
    /// Return a string mostly equal to <paramref name="xml"/>, but with most
    /// control characters replaced by XML entities
    /// </summary>
    public static string FixXml(string xml)
    {
      var hasbad = xml.Any(ch =>
        (ch > 0x00 && ch < 0x09)
        || (ch > 0x0a && ch < 0x0d)
        || (ch > 0x0d && ch < 0x20));
      if(hasbad)
      {
        var sb = new StringBuilder();
        foreach(var ch in xml)
        {
          var v = (ushort)ch;
          if((v > 0x00 && v < 0x09) || (v > 0x0a && v < 0x0d) || (v > 0x0d && v < 0x20))
          {
            sb.Append("&#x");
            sb.Append(v.ToString("X4"));
            sb.Append(";");
          }
          else
          {
            sb.Append(ch);
          }
        }
        return sb.ToString();
      }
      else
      {
        // no patching required, return as is
        return xml;
      }
    }
  }
}
