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
using System.Globalization;

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
      var redb = new RawEventDbV1(dbName, true, true);
      using(var db = redb.Open(true, true))
      {
        db.DbInit();
      }
      Assert.True(File.Exists(dbName));
    }

    [Fact]
    public void CanCreateDbV2()
    {
      var dbName = Path.GetFullPath("cancreatedb-v2.sqlite3");
      _output.WriteLine($"DB file is {dbName}");
      if(File.Exists(dbName))
      {
        _output.WriteLine("Deleting existing DB before test");
        File.Delete(dbName);
      }
      Assert.False(File.Exists(dbName));
      var redb = new RawEventDbV2(dbName, true, true);
      using(var db = redb.Open(true, true))
      {
        db.DbInit();
      }
      Assert.True(File.Exists(dbName));
    }

    [Fact]
    public void ProviderInfoTest()
    {
      var dbName = Path.GetFullPath("providerinfo.sqlite3");
      _output.WriteLine($"DB file is {dbName}");
      if(File.Exists(dbName))
      {
        _output.WriteLine("Deleting existing DB before test");
        File.Delete(dbName);
      }
      Assert.False(File.Exists(dbName));
      var redb = new RawEventDbV2(dbName, true, true);
      using(var db = redb.Open(true, true))
      {
        db.DbInit();
      }
      Assert.True(File.Exists(dbName));
      using(var db = redb.Open(true))
      {
        var providers = db.AllProviderInfoRows().ToList();
        Assert.Empty(providers);
        db.InsertProviderInfoRow(1, "MsiInstaller", null);
        db.InsertProviderInfoRow(2, "MSSQL$SQLEXPRESS", null);
        db.InsertProviderInfoRow(3, "Microsoft-Windows-RestartManager", "{0888e5ef-9b98-4695-979d-e92ce4247224}");
        providers = db.AllProviderInfoRows().ToList();
        Assert.Equal(3, providers.Count);
        Assert.Equal(1, providers[0].ProviderId);
        Assert.Equal(2, providers[1].ProviderId);
        Assert.Equal(3, providers[2].ProviderId);
        Assert.Equal("MsiInstaller", providers[0].ProviderName);
        Assert.Null(providers[0].ProviderGuid);
        Assert.Equal("Microsoft-Windows-RestartManager", providers[2].ProviderName);
        Assert.Equal("{0888e5ef-9b98-4695-979d-e92ce4247224}", providers[2].ProviderGuid);
        var prv = db.FindProvider("MSSQL$SQLEXPRESS");
        Assert.NotNull(prv);
        Assert.Equal(2, prv.ProviderId);
        Assert.Equal("MSSQL$SQLEXPRESS", prv.ProviderName);
        Assert.Null(prv.ProviderGuid);
        prv = db.FindProvider(2);
        Assert.NotNull(prv);
        Assert.Equal(2, prv.ProviderId);
        Assert.Equal("MSSQL$SQLEXPRESS", prv.ProviderName);
        Assert.Null(prv.ProviderGuid);
      }
    }

    private long Stamp(string txt)
    {
      return DateTime.Parse(txt, null, DateTimeStyles.RoundtripKind).TicksSinceEpoch();
    }

    [Fact]
    public void DbV2QueryTest()
    {
      var dbName = Path.GetFullPath("v2query.sqlite3");
      _output.WriteLine($"DB file is {dbName}");
      if(File.Exists(dbName))
      {
        _output.WriteLine("Deleting existing DB before test");
        File.Delete(dbName);
      }
      Assert.False(File.Exists(dbName));
      var redb = new RawEventDbV2(dbName, true, true);
      using(var db = redb.Open(true, true))
      {
        db.DbInit();
      }
      Assert.True(File.Exists(dbName));
      using(var db = redb.Open(true))
      {
        db.InsertProviderInfoRow(4, "Microsoft-Windows-Diagnostics-Performance", "{cfc18ec0-96b1-4eba-961b-622caee05b0a}");
        db.InsertTaskInfoRow(100, 2, 4002, 4, "Boot Performance Monitoring");
        db.InsertTaskInfoRow(200, 1, 4007, 4, "Shutdown Performance Monitoring");
        db.InsertOperationInfoRow(100, 2, 4002, 4, 34, null);
        db.InsertOperationInfoRow(200, 1, 4007, 4, 40, null);
        db.InsertEvent(1L, Stamp("2020-07-26T06:05:14.3403271Z"), 100, 2, 4002, 4, 34, "<Boot1/>");
        db.InsertEvent(2L, Stamp("2020-07-26T06:55:52.0627080Z"), 200, 1, 4007, 4, 40, "<Shutdown1/>");
        db.InsertEvent(241L, Stamp("2022-10-12T06:09:54.0965196Z"), 200, 1, 4007, 4, 40, "<Shutdown2/>");
        db.InsertEvent(242L, Stamp("2022-10-12T06:09:56.0721886Z"), 100, 2, 4002, 4, 34, "<Boot3/>");
      }
      using(var db = redb.Open(false))
      {
        var e1 = db.FindEvent(1L);
        Assert.NotNull(e1);
        Assert.Equal(100, e1.EventId);
        Assert.Equal("<Boot1/>", e1.Xml);
        var h1 = db.FindEventHeader(e1);
        Assert.NotNull(h1);
        Assert.Equal(100, h1.EventId);
        var tir = db.FindTask(e1);
        Assert.NotNull(tir);
        Assert.Equal("Boot Performance Monitoring", tir.TaskDescription);

        var boots = db.QueryEventHeaders(eid: 100, reverse: false).ToList();
        Assert.NotNull(boots);
        Assert.NotEmpty(boots);
        Assert.Equal(2, boots.Count);
        Assert.Equal(1L, boots[0].RecordId);
        Assert.Equal(242L, boots[1].RecordId);
        boots = db.QueryEventHeaders(eid: 100, reverse: true).ToList();
        Assert.NotNull(boots);
        Assert.NotEmpty(boots);
        Assert.Equal(2, boots.Count);
        Assert.Equal(242L, boots[0].RecordId);
        Assert.Equal(1L, boots[1].RecordId);

        var t0 = DateTime.Parse("2020-01-01T00:00:00Z", null, DateTimeStyles.RoundtripKind).TicksSinceEpoch();
        var t1 = DateTime.Parse("2021-01-01T00:00:00Z", null, DateTimeStyles.RoundtripKind).TicksSinceEpoch() - 1L;
        var e2020 = db.QueryEventHeaders(tMin: t0, tMax: t1).ToList();
        Assert.NotNull(e2020);
        Assert.NotEmpty(e2020);
        Assert.Equal(2, e2020.Count);
        Assert.Equal(1L, e2020[0].RecordId);
        Assert.Equal(2L, e2020[1].RecordId);

        var ids = db.QueryEventIds(100, 1000).ToList();
        Assert.NotEmpty(ids);
        Assert.Equal(2, ids.Count);
        Assert.Equal(new [] { 241L, 242L }, ids);
      }
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
      var redb = new RawEventDbV1(dbName, true, true);
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
      var redb = new RawEventDbV1(dbName, true, true);
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
      var redb = new RawEventDbV1(dbName, true, true);
      using(var db = redb.Open(true, true))
      {
        db.DbInit();
      }
      Assert.True(File.Exists(dbName));
      using(var db = redb.Open(true))
      {
        var events = db.ReadEvents().ToList();
        Assert.Empty(events);

        events = db.ReadEvents(ridMin: 0, eid: 1).ToList();
        Assert.Empty(events);

        events = db.ReadEvents(eid: 1).ToList();
        Assert.Empty(events);

        var maxrid = db.MaxRecordId();
        Assert.False(maxrid.HasValue);

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

        maxrid = db.MaxRecordId();
        Assert.True(maxrid.HasValue);
        Assert.Equal(9, maxrid);

        events = db.ReadEvents(ridMin: 3, ridMax: 7).ToList();
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
        // ignore conflict (not a recommended use case)
        db.PutEvent(9, 3, 13, ts0+9*interval, 0, "<dummy/>", ConflictMode.Ignore);
        events = db.ReadEvents().ToList();
        Assert.Equal(9, events[^1].RecordId);
        Assert.Equal(2, events[^1].EventId);
        // overwrite (not a recommended use case)
        db.PutEvent(9, 3, 13, ts0+9*interval, 0, "<dummy/>", ConflictMode.Replace);
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

    [Fact]
    public void EventImportTest()
    {
      var dbName = Path.GetFullPath("import.sqlite3");
      _output.WriteLine($"DB file is {dbName}");
      if(File.Exists(dbName))
      {
        _output.WriteLine("Deleting existing DB before test");
        File.Delete(dbName);
      }
      Assert.False(File.Exists(dbName));
      var redb = new RawEventDbV1(dbName, true, true);
      using(var db = redb.Open(true, true))
      {
        db.DbInit();
      }
      Assert.True(File.Exists(dbName));
      var t0 = new DateTime(2022, 10, 18, 10, 42, 46, 123, DateTimeKind.Utc);
      var ts0 = TimeUtil.TicksSinceEpoch(t0);
      var interval = TimeSpan.FromMilliseconds(1234).Ticks;
      using(var db = redb.Open(true))
      {
        var events = db.ReadEvents().ToList();
        var states = db.ReadEventStates().ToList();
        var tasks = db.ReadTasks().ToList();
        Assert.Empty(events);
        Assert.Empty(states);
        Assert.Empty(tasks);

        using(var eij = new EventImportJob(db))
        {
          eij.ProcessEvent(1, 1, 11, ts0+0*interval, 0, "<dummy/>");
          eij.ProcessEvent(2, 2, 12, ts0+1*interval, 0, "<dummy/>");
          eij.ProcessEvent(3, 1, 11, ts0+2*interval, 0, "<dummy/>");
          eij.ProcessEvent(4, 7, 17, ts0+3*interval, 0, "<dummy/>");
          eij.ProcessEvent(5, 7, 17, ts0+4*interval, 0, "<dummy/>");
          eij.ProcessEvent(6, 1, 11, ts0+5*interval, 0, "<dummy/>");
          eij.ProcessEvent(7, 1, 11, ts0+6*interval, 0, "<dummy/>");
          eij.ProcessEvent(8, 7, 17, ts0+7*interval, 0, "<dummy/>");
          eij.ProcessEvent(9, 2, 12, ts0+8*interval, 0, "<dummy/>");
          // as test: do not commit
        }
        events = db.ReadEvents().ToList();
        Assert.Empty(events);

        using(var eij = new EventImportJob(db))
        {
          eij.ProcessEvent(1, 1, 11, ts0+0*interval, 0, "<dummy/>");
          eij.ProcessEvent(2, 2, 12, ts0+1*interval, 0, "<dummy/>");
          eij.Commit(false);
        }
        events = db.ReadEvents().ToList();
        states = db.ReadEventStates().ToList();
        tasks = db.ReadTasks().ToList();
        Assert.Equal(2, events.Count);
        Assert.Empty(states);
        Assert.Empty(tasks);

        using(var eij = new EventImportJob(db))
        {
          eij.ProcessEvent(3, 1, 11, ts0+2*interval, 0, "<dummy/>");
          eij.ProcessEvent(4, 7, 17, ts0+3*interval, 0, "<dummy/>");
          eij.Commit(true);
        }
        events = db.ReadEvents().ToList();
        states = db.ReadEventStates().ToList();
        tasks = db.ReadTasks().ToList();
        Assert.Equal(4, events.Count);
        Assert.Equal(2, states.Count);
        Assert.Equal(2, tasks.Count);

        using(var eij = new EventImportJob(db))
        {
          eij.ConflictHandling = ConflictMode.Replace;
          eij.ProcessEvent(1, 1, 11, ts0+0*interval, 0, "<dummy/>");
          eij.ProcessEvent(2, 2, 12, ts0+1*interval, 0, "<dummy/>");
          eij.ProcessEvent(3, 1, 11, ts0+2*interval, 0, "<dummy/>");
          eij.ProcessEvent(4, 7, 17, ts0+3*interval, 0, "<dummy/>");
          eij.ProcessEvent(5, 7, 17, ts0+4*interval, 0, "<dummy/>");
          eij.Commit(true);
        }
        events = db.ReadEvents().ToList();
        states = db.ReadEventStates().ToList();
        tasks = db.ReadTasks().ToList();
        Assert.Equal(5, events.Count);
        Assert.Equal(3, states.Count);
        Assert.Equal(3, tasks.Count);

        using(var eij = new EventImportJob(db))
        {
          eij.ProcessEvent(6, 1, 11, ts0+5*interval, 0, "<dummy/>");
          eij.ProcessEvent(7, 1, 11, ts0+6*interval, 0, "<dummy/>");
          eij.ProcessEvent(8, 7, 17, ts0+7*interval, 0, "<dummy/>");
          eij.ProcessEvent(9, 2, 12, ts0+8*interval, 0, "<dummy/>");
          eij.Commit(true);
        }
        events = db.ReadEvents().ToList();
        states = db.ReadEventStates().ToList();
        tasks = db.ReadTasks().ToList();
        Assert.Equal(9, events.Count);
        Assert.Equal(3, states.Count);
        Assert.Equal(3, tasks.Count);

        _output.WriteLine("States:");
        foreach(var state in states)
        {
          _output.WriteLine($"{state.Eid}: v>={state.MinVersion}, enabled={state.Enabled}");
        }
        _output.WriteLine("Tasks:");
        foreach(var task in tasks)
        {
          _output.WriteLine($"({task.EventId},{task.TaskId}) {(task.Description ?? "(no description)")}");
        }
        _output.WriteLine("Events:");
        foreach(var e in events)
        {
          var t = e.TimeStamp.ToString("o");
          _output.WriteLine($"{e.RecordId}: ({e.EventId},{e.TaskId}) {t}");
        }

        _output.WriteLine("Event+Task counts:");
        var etcs = db.EventTaskCounts();
        foreach(var etc in etcs)
        {
          _output.WriteLine($"({etc.EventId},{etc.TaskId}) : {etc.Total}");
        }
      }
    }

    [Fact]
    public void EventLogImportTest()
    {
      var logName = "Microsoft-Windows-Time-Service/Operational";
      var dbName = Path.GetFullPath("import-time.sqlite3");
      _output.WriteLine($"DB file is {dbName}");
      if(File.Exists(dbName))
      {
        _output.WriteLine("Deleting existing DB before test");
        File.Delete(dbName);
      }
      Assert.False(File.Exists(dbName));
      var redb = new RawEventDbV1(dbName, true, true);
      using(var db = redb.Open(true, true))
      {
        db.DbInit();
      }
      Assert.True(File.Exists(dbName));
      using(var db = redb.Open(true))
      {
        var els = EventLogSession.GlobalSession;
        EventLogInformation? info = null;
        try
        {
          info = els.GetLogInformation(logName, PathType.LogName);
        }
        catch(EventLogNotFoundException elnfe)
        {
          Assert.Fail($"Event log not found. {elnfe.Message}");
        }
        catch(UnauthorizedAccessException uae)
        {
          Assert.Fail($"The event log exists but is not accessible to this user. {uae.Message}");
        }
        var recordCount = info.RecordCount ?? 0L;
        var maxrid = db.MaxRecordId();
        _output.WriteLine($"There are {recordCount} records in log '{logName}'. Max DB RID = {maxrid ?? -1}");
        var query =
          maxrid.HasValue
          ? $"*[System/EventRecordID > {maxrid.Value}]"
          : "*";
        var elq = new EventLogQuery(logName, PathType.LogName, query);

        _output.WriteLine($"Reading the first 100 available event records");
        var n = db.PutEvents(ReadRecords(elq), 100, ConflictMode.Default);
        _output.WriteLine($"Stored {n} event records");

        maxrid = db.MaxRecordId();
        _output.WriteLine($"Max DB RID = {maxrid ?? -1}");
        query =
          maxrid.HasValue
          ? $"*[System/EventRecordID > {maxrid.Value}]"
          : "*";
        elq = new EventLogQuery(logName, PathType.LogName, query);

        _output.WriteLine($"Reading the next 700 available event records");
        n = db.PutEvents(ReadRecords(elq), 700, ConflictMode.Default); // test: would throw on failure
        _output.WriteLine($"Stored {n} event records");

        maxrid = db.MaxRecordId();
        _output.WriteLine($"Max DB RID = {maxrid ?? -1}");
        query =
          maxrid.HasValue
          ? $"*[System/EventRecordID > {maxrid.Value}]"
          : "*";
        elq = new EventLogQuery(logName, PathType.LogName, query);

        _output.WriteLine($"Reading 700 more event records");
        n = db.PutEvents(ReadRecords(elq), 700, ConflictMode.Default); // test: would throw on failure
        _output.WriteLine($"Stored {n} event records");

        maxrid = db.MaxRecordId();
        _output.WriteLine($"Max DB RID = {maxrid ?? -1}");
        query =
          maxrid.HasValue
          ? $"*[System/EventRecordID > {maxrid.Value}]"
          : "*";
        elq = new EventLogQuery(logName, PathType.LogName, query);

        _output.WriteLine($"Reading 700 more event records");
        n = db.PutEvents(ReadRecords(elq), 700, ConflictMode.Default); // test: would throw on failure
        _output.WriteLine($"Stored {n} event records");

        Assert.Equal(0, n);
      }
    }

    private IEnumerable<EventLogRecord> ReadRecords(EventLogQuery elq)
    {
      using(var logReader = new EventLogReader(elq))
      {
        EventRecord e;
        while((e = logReader.ReadEvent())!=null)
        {
          if(e.RecordId.HasValue)
          {
            var elr = (EventLogRecord)e;
            yield return elr;
          }
        }
      }
    }

  }
}

