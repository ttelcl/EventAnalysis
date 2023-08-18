/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;

namespace Lcl.EventLog.Utilities
{
  /// <summary>
  /// Utility to fix some conformance errors in the XML generated
  /// by EventLogRecord.ToXml().
  /// </summary>
  public static class XmlUtilities
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

    /// <summary>
    /// Load an XML string into an XPathDocument, using settings 
    /// compatible with Windows Events XML
    /// </summary>
    public static XPathDocument LoadXml(string xml)
    {
      xml = FixXml(xml);
      var settings = new XmlReaderSettings {
        CheckCharacters = false,
      };
      using(var sr = new StringReader(xml))
      using(var xr = XmlReader.Create(sr, settings))
      {
        return new XPathDocument(xr);
      }
    }

    /// <summary>
    /// Returns <paramref name="xml"/> with any text that looks like a default namespace
    /// declaration removed.
    /// </summary>
    public static string StripDefaultNamespaces(string xml)
    {
      return Regex.Replace(xml, "xmlns\\s*=\\s*((\"[^\"]+\")|('[^']+'))", " ");
    }

    /// <summary>
    /// Rewrite an XML string as indented XML text
    /// </summary>
    /// <param name="xml">
    /// The xml string to rewrite
    /// </param>
    /// <param name="fragment">
    /// When true: return an XML fragment. When false: return a document
    /// </param>
    public static string IndentXml(string xml, bool fragment)
    {
      var xpd = LoadXml(xml);
      var settings = new XmlWriterSettings {
        CheckCharacters = false,
        Indent = true,
        ConformanceLevel = fragment ? ConformanceLevel.Fragment : ConformanceLevel.Document,
        Encoding = Encoding.UTF8,
      };
      // Need to use a Stream; using a TextWriter forces a header declaring "utf-16" (even if it isn't)
      byte[] bytes;
      using(var m1 = new MemoryStream())
      {
        using(var xw = XmlWriter.Create(m1, settings))
        {
          if(!fragment)
          {
            xw.WriteStartDocument();
          }
          var nav = xpd.CreateNavigator();
          nav.WriteSubtree(xw);
        }
        if(!fragment)
        {
          m1.WriteByte((byte)'\r');
          m1.WriteByte((byte)'\n');
        }
        bytes = m1.ToArray();
      }
      return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Starts writing XML to the specified file in UTF-8,
    /// already writing the XML declaration.
    /// </summary>
    /// <param name="fileName">
    /// The name of the file to write
    /// </param>
    /// <returns>
    /// The XmlWriter, open for writing. No root element has been
    /// written to it yet.
    /// </returns>
    public static XmlWriter StartXmlFile(string fileName)
    {
      var settings = new XmlWriterSettings {
        CheckCharacters = false,
        Indent = true,
        ConformanceLevel = ConformanceLevel.Document,
        Encoding = Encoding.UTF8,
      };
      return XmlWriter.Create(fileName, settings);
    }

    /// <summary>
    /// Load the XML string and write it to the XML writer according
    /// to the XML writer's settings (e.g. indenting)
    /// </summary>
    /// <param name="xw">
    /// The XML writer to write to, for instance created via <see cref="StartXmlFile(string)"/>
    /// </param>
    /// <param name="xml">
    /// The string representation of the XML to write. This will be "fixed up"
    /// using <see cref="FixXml"/> before loading.
    /// </param>
    public static void AppendXml(this XmlWriter xw, string xml)
    {
      var xpd = LoadXml(xml);
      var nav = xpd.CreateNavigator();
      nav.WriteSubtree(xw);
    }

  }
}
