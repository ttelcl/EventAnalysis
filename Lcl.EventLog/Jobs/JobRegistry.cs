/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lcl.EventLog.Jobs
{
  /// <summary>
  /// Registry of channel jobs for a machine
  /// </summary>
  public class JobRegistry
  {
    private readonly Dictionary<string, EventJobConfig> _byJob;
    private readonly Dictionary<string, EventJobConfig> _byChannel;

    /// <summary>
    /// Create a new JobRegistry. For most use cases consider using
    /// <see cref="EventDataZone.Registry"/> instead of creating your
    /// own instance.
    /// </summary>
    public JobRegistry(string machine)
    {
      _byJob = new Dictionary<string, EventJobConfig>(StringComparer.InvariantCultureIgnoreCase);
      _byChannel = new Dictionary<string, EventJobConfig>(StringComparer.InvariantCultureIgnoreCase);
      Machine = machine;
    }

    /// <summary>
    /// The machine name this registry is about
    /// </summary>
    public string Machine { get; }

    /// <summary>
    /// Register a job configuration. The registration is rejected
    /// if either the job name or channel name are already in use
    /// by a different configuration (but accepted in case both
    /// names match, in which the existing registration is replaced).
    /// </summary>
    /// <param name="cfg">
    /// The job/channel registration object.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if this registration would replace an unrelated existing
    /// registration.
    /// </exception>
    public void Register(EventJobConfig cfg)
    {
      var byChannel = FindByChannel(cfg.Log);
      if(byChannel != null && cfg.Name != byChannel.Name)
      {
        throw new InvalidOperationException(
          $"Duplicate channel name '{cfg.Log}' (jobs '{cfg.Name}' and '{byChannel.Name}')");
      }
      var byJob = FindByJob(cfg.Name);
      if(byJob != null && cfg.Log != byJob.Log)
      {
        throw new InvalidOperationException(
          $"Duplicate job name '{cfg.Name}' (channels '{cfg.Log}' and '{byJob.Log}')");
      }
      _byChannel[cfg.Log] = cfg;
      _byJob[cfg.Name] = cfg;
    }

    /// <summary>
    /// Get the collection of job / channel configurations in this registry
    /// </summary>
    public IReadOnlyCollection<EventJobConfig> Jobs => _byJob.Values;

    /// <summary>
    /// Retrieve a registration by its job name (returning null if not found)
    /// </summary>
    public EventJobConfig? FindByJob(string job)
    {
      if(_byJob.TryGetValue(job, out var config))
      {
        return config;
      }
      else
      {
        return null;
      }
    }

    /// <summary>
    /// Retrieve a registration by its channel name (returning null if not found)
    /// </summary>
    public EventJobConfig? FindByChannel(string channel)
    {
      if(_byChannel.TryGetValue(channel, out var config))
      {
        return config;
      }
      else
      {
        return null;
      }
    }

    internal void Clear()
    {
      _byJob.Clear();
      _byChannel.Clear();
    }
  }
}
