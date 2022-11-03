/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Lcl.EventLog.Utilities.Xml
{
  /// <summary>
  /// A single field query for an event XML document.
  /// Intended for JSON serialization.
  /// </summary>
  public class XmlEventQuery
  {
    /// <summary>
    /// Create a new XmlEventQuery
    /// </summary>
    public XmlEventQuery(
      string label,
      string expression,
      string? transforms = null)
    {
      Label = label;
      Expression = expression;
      Transforms = transforms ?? String.Empty;
    }

    /// <summary>
    /// Deserialize an XmlEventQuery from a JSON string
    /// </summary>
    public static XmlEventQuery FromJson(string json)
    {
      var xeq = JsonConvert.DeserializeObject<XmlEventQuery>(json);
      if(xeq == null)
      {
        throw new InvalidOperationException(
          "'null' is not a valid query specification");
      }
      return xeq;
    }

    /// <summary>
    /// The label for this query / name for this field
    /// </summary>
    [JsonProperty("label")]
    public string Label { get; }

    /// <summary>
    /// The XPath query, optionally prefixed with :sys:, :data: or :udata:.
    /// The query will be executed against the Event XML stripped from default
    /// namespaces.
    /// </summary>
    [JsonProperty("expression")]
    public string Expression { get; }

    /// <summary>
    /// Identifies an optional type or validation transform name, or
    /// comma separated list of names.
    /// </summary>
    [JsonProperty("transforms")]
    public string Transforms { get; }

    /// <summary>
    /// Whether or not the 'transform' field should be serialized
    /// </summary>
    public bool ShouldSerializeTransform()
    {
      return !String.IsNullOrEmpty(Transforms);
    }

    /// <summary>
    /// Evaluate this expression on the XML stored in the provided
    /// dissector.
    /// </summary>
    /// <param name="xml">
    /// The XML loaded in a dissector object
    /// </param>
    /// <param name="transformRegistry">
    /// The registry of available string transformations
    /// </param>
    /// <returns>
    /// The resulting string
    /// </returns>
    public string Evaluate(XmlDissector xml, TransformRegistry transformRegistry)
    {
      var value = xml.Eval(Expression);
      if(!String.IsNullOrEmpty(Transforms))
      {
        var transformNames =
          Transforms!.Split(new[] { ',' })
          .Select(name => name.Trim())
          .Where(name => !string.IsNullOrEmpty(name))
          .ToList();
        foreach(var transformName in transformNames)
        {
          var transform = transformRegistry.Find(transformName);
          if(transform == null)
          {
            throw new KeyNotFoundException(
              $"Unknown string transform '{transformName}' specified for field '{Label}'");
          }
          value = transform.Transform(value, this);
        }
      }
      return value;
    }
  }
}
