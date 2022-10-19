/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace Lcl.EventLog.Jobs
{
  /// <summary>
  /// Models the entire storage for all event data
  /// </summary>
  public class EventDataZone
  {
    /// <summary>
    /// Create a new EventDataZone
    /// </summary>
    /// <param name="readOnly">
    /// Whether or not this DataZone should be treated read-only.
    /// One effect is that when false, this constructor ensures the
    /// RootFolder exists.
    /// </param>
    /// <param name="machine">
    /// The machine name to determine the data zone root folder.
    /// Defaults to the current machine name.
    /// </param>
    /// <param name="baseFolder">
    /// Overrides the base folder to a nonstandard location
    /// </param>
    /// <param name="skipRegistry">
    /// If true: skip job / channel registry initialization.
    /// This allows creating this object even if the job configurations
    /// are corrupt.
    /// </param>
    public EventDataZone(
      bool readOnly,
      string? machine = null,
      string? baseFolder = null,
      bool skipRegistry = false)
    {
      if(String.IsNullOrEmpty(machine))
      {
        machine = Environment.MachineName;
      }
      Machine = machine!;
      ReadOnly = readOnly;
      RootFolder =
        String.IsNullOrEmpty(baseFolder)
        ? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Yoco",
            "EventAnalysis",
            machine)
        : Path.Combine(
            Path.GetFullPath(baseFolder),
            machine);
      Registry = new JobRegistry(Machine);
      if(!ReadOnly && !Directory.Exists(RootFolder))
      {
        Directory.CreateDirectory(RootFolder);
      }
      if(!skipRegistry)
      {
        ReloadRegistry();
      }
    }

    /// <summary>
    /// The root folder for storing events for the specified machine
    /// </summary>
    public string RootFolder { get; }

    /// <summary>
    /// True when this zone should be treated readonly.
    /// </summary>
    public bool ReadOnly { get; }

    /// <summary>
    /// The machine name this data zone relates to
    /// </summary>
    public string Machine { get; }

    /// <summary>
    /// The registry of jobs and channels
    /// </summary>
    public JobRegistry Registry { get; }

    /// <summary>
    /// Clear and reload the job / channel registry
    /// </summary>
    public void ReloadRegistry()
    {
      Registry.Clear();
      foreach(var job in EnumJobs())
      {
        Registry.Register(job);
      }
    }

    /// <summary>
    /// Enumerate and load job configurations found in the zone's root folder
    /// </summary>
    public IEnumerable<EventJobConfig> EnumJobs()
    {
      if(Directory.Exists(RootFolder))
      {
        foreach(var fnm in Directory.EnumerateFiles(RootFolder, "*.evtjob.json"))
        {
          var name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(fnm));
          EventJobConfig cfg = ReadConfigFile(fnm);
          if(cfg.Name != name)
          {
            throw new InvalidOperationException(
              $"Inconsistent name in job '{name}' ({fnm})");
          }
          yield return cfg;
        }
      }
    }

    /// <summary>
    /// Writes a job configuration to the zone. Also registers the configuration
    /// in the Registry.
    /// </summary>
    /// <param name="ejc">
    /// The job configuration to save
    /// </param>
    /// <param name="force">
    /// Default false. Determines the behaviour when the file already exists.
    /// When false, an exception is thrown. When true the existing file is backed
    /// up and the new file is written in its place.
    /// </param>
    public void WriteConfig(EventJobConfig ejc, bool force = false)
    {
      var fnm = JobConfigFile(ejc.Name);
      if(File.Exists(fnm) && !force)
      {
        throw new InvalidOperationException(
          $"File already exists (and force-overwrite flag is not set): {fnm}");
      }
      Registry.Register(ejc);
      var json = JsonConvert.SerializeObject(ejc, Formatting.Indented);
      var tmp = fnm + ".tmp";
      File.WriteAllText(tmp, json);
      if(File.Exists(fnm))
      {
        var bak = fnm + ".bak";
        if(File.Exists(bak))
        {
          File.Delete(bak);
        }
        File.Replace(tmp, fnm, bak);
      }
      else
      {
        File.Move(tmp, fnm);
      }
    }

    /// <summary>
    /// Read a job configuration and wrap it in an EventJob object.
    /// The job to open must have been registered in the Registry already.
    /// </summary>
    /// <param name="name">
    /// The name of the job or the channel.
    /// </param>
    public EventJob OpenJob(string name)
    {
      var ej = TryOpenJob(name);
      if(ej == null)
      {
        throw new InvalidOperationException(
          $"Unknown job or channel: '{name}'");
      }
      else
      {
        return ej;
      }
    }

    /// <summary>
    /// Read a job configuration and wrap it in an EventJob object.
    /// Returns null if not found
    /// The job to open must have been registered in the Registry already.
    /// </summary>
    /// <param name="name">
    /// The name of the job or the channel.
    /// </param>
    public EventJob? TryOpenJob(string name)
    {
      EventJobConfig? cfg;
      if(EventJobConfig.IsValidJobName(name))
      {
        cfg = Registry.FindByJob(name) ?? Registry.FindByChannel(name);
      }
      else
      {
        cfg = Registry.FindByChannel(name);
      }
      if(cfg == null)
      {
        return null;
      }
      else
      {
        return new EventJob(this, cfg);
      }
    }

    /// <summary>
    /// Get the name of a job's config file
    /// </summary>
    public string JobConfigFile(string name)
    {
      EventJobConfig.ThrowIfInvalidJobName(name);
      return Path.Combine(RootFolder, name + ".evtjob.json");
    }

    private static EventJobConfig ReadConfigFile(string fnm)
    {
      var json = File.ReadAllText(fnm);
      var cfg = JsonConvert.DeserializeObject<EventJobConfig>(json);
      if(cfg == null)
      {
        throw new InvalidOperationException(
          $"Error loading job file ({Path.GetFileName(fnm)})");
      }
      return cfg;
    }
  }
}
