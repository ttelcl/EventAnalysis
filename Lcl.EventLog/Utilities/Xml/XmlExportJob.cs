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
  /// Describes a set of "properties" that can be extracted from an XML event record,
  /// typically for a specific event ID. This class is JSON serializable
  /// </summary>
  public class XmlExportJob
  {
    /// <summary>
    /// Create a new XmlExportDescriptor
    /// </summary>
    public XmlExportJob(
      string jobname,
      IEnumerable<int> events,
      IDictionary<string, ProtoXmlEventQuery> queries)
    {
      JobName = jobname;
      Events = events.ToList().AsReadOnly();
      var q1 = new Dictionary<string, ProtoXmlEventQuery>();
      var q2 = new Dictionary<string, XmlEventQuery>();
      foreach(var kvp in queries)
      {
        q1[kvp.Key] = kvp.Value;
        q2[kvp.Key] = new XmlEventQuery(kvp.Key, kvp.Value.Expression, kvp.Value.Transforms);
      }
      Queries = q1;
      Queries2 = q2;
    }

    /// <summary>
    /// The name for this job
    /// </summary>
    [JsonProperty("jobname")]
    public string JobName { get; }

    /// <summary>
    /// The event IDs this export job applies to. Typically only one.
    /// The special case of an empty list can be used to indicate "all"
    /// events (which only makes sense when looking exclusively at common
    /// fields)
    /// </summary>
    [JsonProperty("events")]
    public IReadOnlyList<int> Events { get; }

    /// <summary>
    /// The serialized form of the queries
    /// </summary>
    [JsonProperty("queries")]
    public IReadOnlyDictionary<string, ProtoXmlEventQuery> Queries { get; }

    /// <summary>
    /// The fully rehydrated queries
    /// </summary>
    [JsonIgnore]
    public IReadOnlyDictionary<string, XmlEventQuery> Queries2 { get; }
  }
}