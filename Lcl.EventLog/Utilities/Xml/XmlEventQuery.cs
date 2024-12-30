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

namespace Lcl.EventLog.Utilities.Xml;

/// <summary>
/// The structure of XmlEventQuery minus the Label; used for serialization
/// when the label is implied by a dictionary key
/// </summary>
public class ProtoXmlEventQuery
{
  /// <summary>
  /// Create a ProtoXmlEventQuery
  /// </summary>
  public ProtoXmlEventQuery(
    string expression,
    string? transforms = null)
  {
    Expression = expression;
    Transforms = transforms ?? String.Empty;
  }

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
  public bool ShouldSerializeTransforms()
  {
    return !String.IsNullOrEmpty(Transforms);
  }

}

/// <summary>
/// A single field query for an event XML document.
/// Intended for JSON serialization.
/// </summary>
public class XmlEventQuery : ProtoXmlEventQuery
{
  /// <summary>
  /// Create a new XmlEventQuery
  /// </summary>
  public XmlEventQuery(
    string label,
    string expression,
    string? transforms = null)
    : base(expression, transforms)
  {
    Label = label;
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
  /// Parse a query plus transforms from a compact string representation
  /// </summary>
  /// <param name="label">
  /// The label for this query / name for this field
  /// </param>
  /// <param name="text">
  /// The compact string representation of the query, in the form
  /// "expression;transforms"
  /// </param>
  /// <returns></returns>
  public static XmlEventQuery FromCompactString(string label, string text)
  {
    var parts = text.Split(';');
    if(parts.Length > 2)
    {
      throw new InvalidOperationException(
        "Compact query string must contain at most one semicolon");
    }
    var expression = parts[0];
    var transforms = parts.Length > 1 ? parts[1] : null;
    return new XmlEventQuery(label, expression, transforms);
  }

  /// <summary>
  /// Create a collection of XmlEventQuery objects from a JSON object,
  /// mapping keys to queries (using the key as the query label)
  /// </summary>
  public static IDictionary<string, XmlEventQuery> FromCompactStringMap(
    string json)
  {
    var map = JsonConvert.DeserializeObject<IDictionary<string, string>>(json);
    return map!.ToDictionary(
      kvp => kvp.Key,
      kvp => FromCompactString(kvp.Key, kvp.Value));
  }

  /// <summary>
  /// The label for this query / name for this field
  /// </summary>
  [JsonProperty("label")]
  public string Label { get; }

  /// <summary>
  /// Create an evaluator for this query
  /// </summary>
  /// <param name="transformRegistry">
  /// The registry of available string transformations.
  /// Must contain all transformations specified in this query.
  /// </param>
  /// <returns></returns>
  public XmlEventQueryEvaluator CreateEvaluator(TransformRegistry transformRegistry)
  {
    return new XmlEventQueryEvaluator(this, transformRegistry);
  }
}

/// <summary>
/// A query evaluator for an XmlEventQuery, combining the JSON-compatible
/// <see cref="XmlEventQuery"/> with its pre-loaded <see cref="XmlFieldTransform"/>s
/// </summary>
public class XmlEventQueryEvaluator
{
  /// <summary>
  /// Create a new XmlEventQueryEvaluator
  /// </summary>
  /// <param name="source">
  /// The deserialized query
  /// </param>
  /// <param name="transformRegistry">
  /// The registry of available string transformations.
  /// All transformations specified in the query must exist this registry.
  /// </param>
  public XmlEventQueryEvaluator(
    XmlEventQuery source,
    TransformRegistry transformRegistry)
  {
    Source = source;
    FieldTransforms = source.Transforms
      .Split(new[] { ',' })
      .Select(name => name.Trim())
      .Where(name => !string.IsNullOrEmpty(name))
      .Select(name =>
        transformRegistry.Find(name)
        ?? throw new InvalidOperationException(
          $"Unknown field transformation '{name}'"))
      .ToList();
  }

  /// <summary>
  /// The query to evaluate
  /// </summary>
  public XmlEventQuery Source { get; }

  /// <summary>
  /// The transforms to apply to the query result (possibly empty)
  /// </summary>
  public IReadOnlyList<XmlFieldTransform> FieldTransforms { get; }

  /// <summary>
  /// Evaluate the query against the provided XML data (wrapped in
  /// a dissector)
  /// </summary>
  public string Evaluate(XmlDissector xml)
  {
    var value = xml.Eval(Source.Expression);
    foreach(var transform in FieldTransforms)
    {
      value = transform.Transform(value, Source);
    }
    return value;
  }
}
