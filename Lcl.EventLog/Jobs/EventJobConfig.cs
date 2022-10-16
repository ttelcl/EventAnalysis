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

namespace Lcl.EventLog.Jobs
{
  /// <summary>
  /// Event extraction job configuration DTO
  /// </summary>
  public class EventJobConfig
  {
    /// <summary>
    /// Create a new EventJob
    /// </summary>
    public EventJobConfig(
      string name,
      EventLogSource source)
    {
      Name = name;
      Source = source;
    }

    /// <summary>
    /// The name of the job (also used in file names)
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; }

    /// <summary>
    /// The event source
    /// </summary>
    [JsonProperty("source")]
    public EventLogSource Source { get; }

    /// <summary>
    /// Inner object specifying the event source
    /// </summary>
    public class EventLogSource
    {
      private List<int> _events;

      /// <summary>
      /// Create a new EventJobConfig.Source
      /// </summary>
      public EventLogSource(string log, IEnumerable<int>? events = null)
      {
        _events = events == null ? new List<int>() : new List<int>(events);
        Events = _events.AsReadOnly();
        Log = log;
      }

      /// <summary>
      /// The name of the event log to import from
      /// </summary>
      [JsonProperty("log")]
      public string Log { get; }

      /// <summary>
      /// The event types to import, or an empty list to import all
      /// </summary>
      [JsonProperty("events")]
      public IReadOnlyList<int> Events { get; }

      /// <summary>
      /// Determines if Events should be serialized (it is not serialized when empty)
      /// </summary>
      public bool ShouldSerializeEvents()
      {
        return Events.Any();
      }
    }

  }
}
