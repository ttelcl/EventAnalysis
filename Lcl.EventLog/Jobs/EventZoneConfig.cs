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
  /// Configuration DTO for a machine data zone
  /// </summary>
  public class EventZoneConfig
  {
    /// <summary>
    /// Create a new EventZoneConfig
    /// </summary>
    public EventZoneConfig(
      string machine)
    {
      Machine = machine.ToUpperInvariant();
    }

    /// <summary>
    /// The machine name (= zone name)
    /// </summary>
    [JsonProperty("machine")]
    public string Machine { get; }

  }
}
