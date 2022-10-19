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
  /// Event extraction job configuration DTO.
  /// </summary>
  /// <remarks>
  /// <para>
  /// These are persisted as *.evtjob.json files in an
  /// <see cref="EventDataZone"/>
  /// </para>
  /// </remarks>
  public class EventJobConfig
  {
    /// <summary>
    /// Create a new EventJob
    /// </summary>
    /// <param name="name">
    /// The job name (used as short form to identify a log channel).
    /// This must be valid according to <see cref="IsValidJobName(string)"/>.
    /// </param>
    /// <param name="channel">
    /// The log channel name
    /// </param>
    /// <param name="admin">
    /// A flag that advises the user that the windows event log channel
    /// requires special priviliges.
    /// </param>
    public EventJobConfig(
      string name,
      string channel,
      bool admin)
    {
      Name = name;
      Channel = channel;
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
    /// The name of the event log channel to import from
    /// </summary>
    [JsonProperty("channel")]
    public string Channel { get; }

    /// <summary>
    /// Whether accessing the underlying event log requires
    /// admin privileges
    /// </summary>
    [JsonProperty("admin")]
    public bool Admin { get; }
  }
}
