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

namespace Lcl.EventLog.Jobs
{
  /// <summary>
  /// The logic related to handling event jobs
  /// </summary>
  public class EventJob
  {
    /// <summary>
    /// Create a new EventJob
    /// </summary>
    public EventJob(
      EventDataZone zone,
      EventJobConfig configuration)
    {
      Zone = zone;
      Configuration = configuration;
      JobFolder = Path.Combine(Zone.RootFolder, Configuration.Name);
      RawDbFile = Path.Combine(JobFolder, $"{Configuration.Name}.raw-events.sqlite3");
    }

    /// <summary>
    /// The event zone storing the data
    /// </summary>
    public EventDataZone Zone { get; }

    /// <summary>
    /// The event job configuration
    /// </summary>
    public EventJobConfig Configuration { get; }

    /// <summary>
    /// The folder for the job data
    /// </summary>
    public string JobFolder { get; }

    /// <summary>
    /// The filename for the raw event import DB
    /// </summary>
    public string RawDbFile { get; }

    private void InitFolder()
    {
      if(!Directory.Exists(JobFolder))
      {
        Directory.CreateDirectory(JobFolder);
      }
    }

  }
}
