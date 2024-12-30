/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Lcl.EventLog.Utilities.Xml
{
  /// <summary>
  /// Wraps an XML document (or fragment) and provides access to it via
  /// XPath expressions. Optionally strips namespace information upon
  /// construction, to simplify the XPath expressions.
  /// </summary>
  public class XmlDissector
  {
    private readonly XPathDocument _xpdoc;

    /// <summary>
    /// Create a new XmlDissector, removing any default namespace declarations
    /// before loading
    /// </summary>
    public XmlDissector(string xml)
    {
      xml = XmlUtilities.StripDefaultNamespaces(xml);
      _xpdoc = XmlUtilities.LoadXml(xml); // also handles control characters
    }

    /// <summary>
    /// Create a new XPathNavigator on the XPathDocument
    /// </summary>
    public XPathNavigator CreateNavigator()
    {
      return _xpdoc.CreateNavigator();
    }

    /// <summary>
    /// Evaluate the XPath expression to a string.
    /// The expression is wrapped in a "string()" XPath function call.
    /// Non-existent nodes will result in an empty string being returned.
    /// The prefixes :sys:, :data:, and :udata: transform the expression like
    /// EvalSystem, EvalData and EvalUserData
    /// </summary>
    /// <remarks>
    /// Since XPath has no concept of "null", distinguishing a missing node from
    /// an empty one is not trivial.
    /// </remarks>
    public string Eval(string xpath)
    {
      if(xpath.StartsWith(":"))
      {
        var parts = xpath.Split(new[] { ':' }, 3);
        if(parts.Length == 3)
        {
          var prefixKey = parts[1];
          var suffix = parts[2];
          xpath = prefixKey switch {
            "common" => XpathCommon(suffix),
            "sys" => "/Event/System/" + suffix,
            "data" => DataXpath(suffix),
            "udata" => "/Event/UserData/*/" + suffix,
            _ => throw new InvalidOperationException(
                            $"Unrecognized expression prefix ':{prefixKey}:'"),
          };
        }
      }
      return (string)CreateNavigator().Evaluate("string(" + xpath + ")");
    }

    private static string XpathCommon(string key)
    {
      return key switch {
        "Rid" => "/Event/System/EventRecordID",
        "Provider" => "/Event/System/Provider/@Name",
        "ProviderGuid" => "/Event/System/Provider/@Guid",
        "EventId" => "/Event/System/EventID",
        "Task" => "/Event/System/Task",
        "Time" => "/Event/System/TimeCreated/@SystemTime",
        "UserSid" => "/Event/System/Security/@UserID",
        _ => throw new InvalidOperationException(
                        $"Unrecognized common expression ':common:{key}'"),
      };
    }

    /// <summary>
    /// Return a dictionary mapping the data values to their values.
    /// If the document has no values, an empty dictionary is returned.
    /// If the data values have no names, names are synthesised based on the
    /// element indices, using 1-based indexes (for xpath compatibility)
    /// </summary>
    public Dictionary<string, string> MapData()
    {
      var nav = CreateNavigator();
      var map = new Dictionary<string, string>();
      var eventData = nav.SelectSingleNode("/Event/EventData");
      if(eventData != null)
      {
        var dataElements = eventData.SelectChildren("Data", eventData.NamespaceURI);
        if(dataElements != null)
        {
          var index = 1;
          while(dataElements.MoveNext())
          {
            var name = (string?)(dataElements.Current!.Evaluate("string(@Name)"));
            if(String.IsNullOrEmpty(name))
            {
              name = "$X" + index.ToString("D2");
            }
            var value = dataElements.Current.InnerXml;
            map[name!] = value;
            index++;
          }
        }
      }
      return map;
    }

    /// <summary>
    /// Like eval, but throws an exception if the return value is null or empty
    /// </summary>
    public string EvalNotEmpty(string xpath)
    {
      var value = Eval(xpath);
      if(String.IsNullOrEmpty(value))
      {
        throw new InvalidOperationException(
          $"Required content is missing for query: {xpath}");
      }
      return value;
    }

    /// <summary>
    /// Evaluate the value of an element or attribute in the /Event/System branch
    /// </summary>
    /// <param name="element">
    /// The name of the element to evaluate. You can also specify a partial XPath
    /// expressions such as "MyElement/@MyAttribute"
    /// </param>
    /// <param name="attribute">
    /// The name of the attribute inside the selected element to evaluate, or null
    /// (default) to evaluate the element itself.
    /// </param>
    public string EvalSystem(string element, string? attribute = null)
    {
      return String.IsNullOrEmpty(attribute)
        ? Eval($"/Event/System/{element}")
        : Eval($"/Event/System/{element}/@{attribute}");
    }

    /// <summary>
    /// Evaluate the value of an element in the /Event/EventData branch
    /// </summary>
    /// <param name="nameOrIndexKey">
    /// The name or the index key. If this has the shape "$X" followed
    /// by 2 or more digits, the digits are interpreted as a data index.
    /// Otherwise this parameter is interpreted as data field name
    /// </param>
    public string EvalData(string nameOrIndexKey)
    {
      var xpath = DataXpath(nameOrIndexKey);
      return Eval(xpath);
    }

    /// <summary>
    /// Convert a data key into an xpath expression, supporting both
    /// named and indexed keys
    /// </summary>
    /// <param name="nameOrIndexKey">
    /// The name or the index key. If this has the shape "$X" followed
    /// by 2 or more digits, the digits are interpreted as a data index.
    /// Otherwise this parameter is interpreted as data field name
    /// </param>
    public static string DataXpath(string nameOrIndexKey)
    {
      var match = Regex.Match(nameOrIndexKey, @"^\$X(\d\d+)$");
      if(match.Success)
      {
        var index = Int32.Parse(match.Groups[1].Value);
        return $"/Event/EventData/Data[{index}]";
      }
      else
      {
        return $"/Event/EventData/Data[@Name='{nameOrIndexKey}']";
      }
    }

    /// <summary>
    /// Evaluate the value of an element in the 
    /// /Event/UserData/*/ branch
    /// </summary>
    public string EvalUserData(string element, string? attribute = null)
    {
      return String.IsNullOrEmpty(attribute)
        ? Eval($"/Event/UserData/*/{element}")
        : Eval($"/Event/UserData/*/{element}/@{attribute}");
    }

  }
}
