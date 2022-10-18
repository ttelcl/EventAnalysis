﻿/*
 * (c) 2022  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

using Lcl.EventLog.Jobs.Database;
using Lcl.EventLog.Utilities;

namespace UnitTest.Lcl.EventLog
{
  public class DbTests
  {
    private readonly ITestOutputHelper _output;

    public DbTests(ITestOutputHelper output)
    {
      _output = output;
    }

    [Fact]
    public void CanCreateDb()
    {
      var dbName = Path.GetFullPath("cancreatedb.sqlite3");
      _output.WriteLine($"DB file is {dbName}");
      if(File.Exists(dbName))
      {
        _output.WriteLine("Deleting existing DB before test");
        File.Delete(dbName);
      }
      Assert.False(File.Exists(dbName));
      var redb = new RawEventDb(dbName, true, true);
      using(var db = redb.Open(true, true))
      {
        db.DbInit();
      }
      Assert.True(File.Exists(dbName));
    }

    [Fact]
    public void EventStatesTableTest()
    {
      var dbName = Path.GetFullPath("eventstates.sqlite3");
      _output.WriteLine($"DB file is {dbName}");
      if(File.Exists(dbName))
      {
        _output.WriteLine("Deleting existing DB before test");
        File.Delete(dbName);
      }
      Assert.False(File.Exists(dbName));
      var redb = new RawEventDb(dbName, true, true);
      using(var db = redb.Open(true, true))
      {
        db.DbInit();
      }
      Assert.True(File.Exists(dbName));
      using(var db = redb.Open(true))
      {
        db.DbInit();
        var states = db.ReadEventStates().ToList();
        Assert.Empty(states);

        db.PutEventState(1, 1, true);
        db.PutEventState(2, 1, false);
        states = db.ReadEventStates().ToList();
        Assert.Equal(2, states.Count);
        Assert.Equal(1, states[0].Eid);
        Assert.Equal(1, states[0].MinVersion);
        Assert.True(states[0].Enabled);
        Assert.Equal(2, states[1].Eid);
        Assert.Equal(1, states[1].MinVersion);
        Assert.False(states[1].Enabled);

        db.EnableEventState(1, false);
        db.EnableEventState(2, true);
        db.EnableEventState(3, true);
        states = db.ReadEventStates().ToList();
        Assert.Equal(3, states.Count);
        Assert.Equal(1, states[0].Eid);
        Assert.Equal(1, states[0].MinVersion);
        Assert.False(states[0].Enabled);
        Assert.Equal(2, states[1].Eid);
        Assert.Equal(1, states[1].MinVersion);
        Assert.True(states[1].Enabled);
        Assert.Equal(3, states[2].Eid);
        Assert.Equal(0, states[2].MinVersion);
        Assert.True(states[2].Enabled);

        db.SetMinVersion(3, 2);
        db.SetMinVersion(4, 2);
        states = db.ReadEventStates().ToList();
        Assert.Equal(4, states.Count);
        Assert.Equal(3, states[2].Eid);
        Assert.Equal(2, states[2].MinVersion);
        Assert.True(states[2].Enabled);
        Assert.Equal(4, states[3].Eid);
        Assert.Equal(2, states[3].MinVersion);
        Assert.True(states[3].Enabled);
      }
    }

    [Fact]
    public void TasksTableTest()
    {
      var dbName = Path.GetFullPath("tasks.sqlite3");
      _output.WriteLine($"DB file is {dbName}");
      if(File.Exists(dbName))
      {
        _output.WriteLine("Deleting existing DB before test");
        File.Delete(dbName);
      }
      Assert.False(File.Exists(dbName));
      var redb = new RawEventDb(dbName, true, true);
      using(var db = redb.Open(true, true))
      {
        db.DbInit();
      }
      Assert.True(File.Exists(dbName));

      using(var db = redb.Open(true))
      {
        var tasks = db.ReadTasks().ToList();
        Assert.Empty(tasks);

        db.PutTask(100, 4002, "Boot Performance Monitoring");
        db.PutTask(200, 4007, "Shutdown Performance Monitoring");
        db.PutTask(4688, 13312, "Process Creation");
        db.PutTask(4689, 13313, "Process Termination");
        db.PutTask(2, 1, null);
        db.PutTask(1, 1, null);
        tasks = db.ReadTasks().ToList();
        Assert.Equal(6, tasks.Count);
        Assert.Equal(1, tasks[0].EventId);
        Assert.Equal(1, tasks[0].TaskId);
        Assert.Null(tasks[0].Description);
        Assert.Equal(4689, tasks[5].EventId);
        Assert.Equal(13313, tasks[5].TaskId);
        Assert.Equal("Process Termination", tasks[5].Description);

        db.PutTask(1, 1, "Some setup stuff");
        db.PutTask(4689, 13313, null);
        tasks = db.ReadTasks().ToList();
        Assert.Equal(6, tasks.Count);
        Assert.Equal("Some setup stuff", tasks[0].Description);
        Assert.Null(tasks[5].Description);
      }
    }

    [Fact]
    public void EventsTableTest()
    {
      var dbName = Path.GetFullPath("events.sqlite3");
      _output.WriteLine($"DB file is {dbName}");
      if(File.Exists(dbName))
      {
        _output.WriteLine("Deleting existing DB before test");
        File.Delete(dbName);
      }
      Assert.False(File.Exists(dbName));
      var redb = new RawEventDb(dbName, true, true);
      using(var db = redb.Open(true, true))
      {
        db.DbInit();
      }
      Assert.True(File.Exists(dbName));
      using(var db = redb.Open(true))
      {
        var events = db.ReadEvents().ToList();
        Assert.Empty(events);

        events = db.ReadEvents(ridMin:0, eid:1).ToList();
        Assert.Empty(events);

        events = db.ReadEvents(eid: 1).ToList();
        Assert.Empty(events);

        var t0 = new DateTime(2022, 10, 17, 10, 30, 46, 123, DateTimeKind.Utc);
        var ts0 = TimeUtil.TicksSinceEpoch(t0);
        var interval = TimeSpan.FromMilliseconds(1234).Ticks;
        db.PutEvent(1, 1, 11, ts0+0*interval, 0, "<dummy/>");
        db.PutEvent(2, 2, 12, ts0+1*interval, 0, "<dummy/>");
        db.PutEvent(3, 1, 11, ts0+2*interval, 0, "<dummy/>");
        // insert events out-of-order, so we can verify they are returned in-order
        db.PutEvent(7, 1, 11, ts0+6*interval, 0, "<dummy/>");
        db.PutEvent(8, 7, 17, ts0+7*interval, 0, "<dummy/>");
        db.PutEvent(9, 2, 12, ts0+8*interval, 0, "<dummy/>");
        db.PutEvent(4, 7, 17, ts0+3*interval, 0, "<dummy/>");
        db.PutEvent(5, 7, 17, ts0+4*interval, 0, "<dummy/>");
        db.PutEvent(6, 1, 11, ts0+5*interval, 0, "<dummy/>");

        events = db.ReadEvents().ToList();
        Assert.Equal(9, events.Count);
        Assert.Equal(1, events[0].RecordId);
        Assert.Equal(1, events[0].EventId);
        Assert.Equal("2022-10-17T10:30:46.1230000Z", events[0].TimeStamp.ToString("o"));
        Assert.Equal(9, events[^1].RecordId);
        Assert.Equal(2, events[^1].EventId);

        events = db.ReadEvents(ridMin:3, ridMax:7).ToList();
        Assert.Equal(5, events.Count);
        Assert.Equal(3, events[0].RecordId);
        Assert.Equal(7, events[^1].RecordId);

        events = db.ReadEvents(ridMin: 3, ridMax: 7, eid: 7).ToList();
        Assert.Equal(2, events.Count);
        Assert.Equal(4, events[0].RecordId);
        Assert.Equal(5, events[^1].RecordId);

        events = db.ReadEvents(ridMax: 7).ToList();
        Assert.Equal(7, events.Count);
        Assert.Equal(1, events[0].RecordId);
        Assert.Equal(7, events[^1].RecordId);

        // duplicate detection
        Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() => {
          db.PutEvent(9, 3, 13, ts0+9*interval, 0, "<dummy/>");
        });
        // overwrite (not a recommended use case)
        db.PutEvent(9, 3, 13, ts0+9*interval, 0, "<dummy/>", true);
        events = db.ReadEvents().ToList();
        Assert.Equal(9, events[^1].RecordId);
        Assert.Equal(3, events[^1].EventId);

        _output.WriteLine("-----------");
        foreach(var e in events)
        {
          var ts = e.TimeStamp.ToString("o");
          _output.WriteLine($"R:{e.RecordId} E:{e.EventId} T:{ts}");
        }

        // DateTime based filtering
        var utc0 = new DateTime(2022, 10, 17, 10, 30, 50, DateTimeKind.Utc);
        events = db.ReadEvents(utcMin: utc0, utcMax: utc0.AddSeconds(5)).ToList();
        Assert.Equal(4, events.Count);
        Assert.Equal(5, events[0].RecordId);
        Assert.Equal(8, events[^1].RecordId);
      }

    }


  }
}

