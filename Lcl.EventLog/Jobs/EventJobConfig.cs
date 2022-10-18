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
      string log,
      bool admin)
    {
      Name = name;
      Log = log;
      Admin = admin;
      ThrowIfInvalidJobName(Name);
    }

    /// <summary>
    /// Returns true if the provided name is valid as job name.
    /// </summary>
    public static bool IsValidJobName(string name)
    {
      var rgx = @"[a-z][a-z0-9]*([-_][a-z0-9]+)*";
      return Regex.IsMatch(name, rgx, RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Throw an InvalidOperationException if the argument is not a valid job name
    /// </summary>
    public static void ThrowIfInvalidJobName(string name)
    {
      if(!IsValidJobName(name))
      {
        throw new InvalidOperationException(
          $"Not a valid job name: '{name}'");
      }
    }

    /// <summary>
    /// The name of the job (also used in file names)
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; }

    /// <summary>
    /// The name of the event log to import from
    /// </summary>
    [JsonProperty("log")]
    public string Log { get; }

    /// <summary>
    /// Whether accessing the underlying event log requires
    /// admin privileges
    /// </summary>
    [JsonProperty("admin")]
    public bool Admin { get; }
  }
}
