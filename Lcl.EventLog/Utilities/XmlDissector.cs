/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;

namespace Lcl.EventLog.Utilities
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
    /// Evaluate the XPath expression to a string. The expression is wrapped in
    /// a "string()" XPath function call.
    /// Non-existent nodes will result in an empty string being returned.
    /// </summary>
    /// <remarks>
    /// Since XPath has no concept of "null", distinguishing a missing node from
    /// an empty one is not trivial.
    /// </remarks>
    public string Eval(string xpath)
    {
      return (string)CreateNavigator().Evaluate("string(" + xpath + ")");
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
    public string EvalSystem(string element, string? attribute=null)
    {
      return String.IsNullOrEmpty(attribute)
        ? Eval($"/Event/System/{element}")
        : Eval($"/Event/System/{element}/@{attribute}");
    }

    /// <summary>
    /// Evaluate the value of an element in the /Event/EventData branch
    /// </summary>
    public string EvalData(string name)
    {
      return Eval($"/Event/EventData/Data[@Name='{name}']");
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
