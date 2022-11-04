/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

using Lcl.EventLog.Jobs.Database;
using Lcl.EventLog.Utilities;
using Lcl.EventLog.Jobs;

namespace UnitTest.Lcl.EventLog
{
  public class ZoneTests
  {
    private readonly ITestOutputHelper _output;

    public ZoneTests(ITestOutputHelper output)
    {
      _output = output;
    }

    private void RecursiveDeleteFolder(DirectoryInfo di)
    {
      // ref https://stackoverflow.com/a/22282428/271323
      if(di.Exists)
      {
        _output.WriteLine($"Deleting {di.FullName}");
        foreach(var dir in di.EnumerateDirectories())
        {
          RecursiveDeleteFolder(dir);
        }
        di.Delete(true);
      }
    }

    [Fact]
    public void CanCreateZoneAndDatabase()
    {
      var baseFolder = Environment.CurrentDirectory;
      var machine = "TestZone01";
      var zoneFolder = Path.Combine(baseFolder, machine);
      if(Directory.Exists(zoneFolder))
      {
        RecursiveDeleteFolder(new DirectoryInfo(zoneFolder));
      }
      Assert.False(Directory.Exists(zoneFolder));
      var edz = new EventDataZone(false, machine, baseFolder);
      Assert.True(Directory.Exists(zoneFolder));
      var configs = edz.EnumJobs().ToList();
      Assert.Empty(configs);
      edz.WriteConfig(
        new EventJobConfig("testing", "Microsoft-Windows-Time-Service/Operational", false),
        true);
      configs = edz.EnumJobs().ToList();
      Assert.Single(configs);
      var testingJob = edz.TryOpenJob("testing");
      Assert.NotNull(testingJob);
      var fooJob = edz.TryOpenJob("foo");
      Assert.Null(fooJob);
      Assert.False(testingJob.HasDbV1);
      var redb = testingJob.OpenDatabase(true);
      Assert.NotNull(redb);
      Assert.True(testingJob.HasDbV1);

      Assert.Throws<InvalidOperationException>(
        () => {
          // Cannot create anything here anymore, so the second "true" is disallowed
          using(var db = redb.Open(true, true))
          {
          }
        });
      using(var db = redb.Open(true))
      {
        var states = db.ReadEventStates();
        Assert.Empty(states);
      }
    }

    [Fact]
    public void CanFillDatabase()
    {
      var baseFolder = Environment.CurrentDirectory;
      var machine = "TestZone02";
      var jobName = "filling";
      var zoneFolder = Path.Combine(baseFolder, machine);
      if(Directory.Exists(zoneFolder))
      {
        RecursiveDeleteFolder(new DirectoryInfo(zoneFolder));
      }
      Assert.False(Directory.Exists(zoneFolder));
      var edz = new EventDataZone(false, machine, baseFolder);
      Assert.True(Directory.Exists(zoneFolder));
      edz.WriteConfig(
        new EventJobConfig(jobName, "Microsoft-Windows-Time-Service/Operational", false),
        true);
      var fillingJob = edz.TryOpenJob(jobName);
      Assert.NotNull(fillingJob);
      Assert.False(fillingJob.HasDbV1);
      using(var db = fillingJob.OpenInnerDatabase(true))
      {
        Assert.True(fillingJob.HasDbV1);
        var states = db.ReadEventStates();
        Assert.Empty(states);
        var n = fillingJob.UpdateDb(db);
        _output.WriteLine($"Inserted {n} records");
        var overviews = db.GetOverview();
        foreach(var o in overviews)
        {
          var label = o.TaskLabel ?? "?";
          var tMin = o.UtcMin?.ToLocalTime().ToString("o") ?? "?";
          var tMax = o.UtcMax?.ToLocalTime().ToString("o") ?? "?";
          _output.WriteLine(
            $"({o.EventId}, {o.TaskId}): {o.EventCount,4}, {o.MinRid,5} - {o.MaxRid,5}, {o.IsEnabled}, '{label}', {tMax}");
        }
      }
    }

  }
}
