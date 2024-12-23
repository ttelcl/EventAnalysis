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

using Lcl.EventLog.Jobs.Database;
using Lcl.EventLog.Utilities;

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
      RawDbFileV1 = Path.Combine(JobFolder, $"{Configuration.Name}.raw-events.sqlite3");
      RawDbFileV2 = Path.Combine(JobFolder, $"{Configuration.Name}.raw.sqlite3");
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
    /// The filename for the raw event import DB, original version
    /// </summary>
    public string RawDbFileV1 { get; }

    /// <summary>
    /// The filename for the raw event import DB, new version
    /// </summary>
    public string RawDbFileV2 { get; }

    /// <summary>
    /// True if the V1 database file exists
    /// </summary>
    public bool HasDbV1 => File.Exists(RawDbFileV1);

    /// <summary>
    /// True if the V2 database file exists
    /// </summary>
    public bool HasDbV2 => File.Exists(RawDbFileV2);

    /// <summary>
    /// Insert missing records into the database from the event log, taking into
    /// account the filter settings. This overload opens the inner db, performs
    /// the update, then closes it again.
    /// </summary>
    /// <param name="cap">
    /// The maximum number of records to insert
    /// </param>
    /// <returns>
    /// The number of records inserted.
    /// </returns>
    /// <exception cref="EventLogNotFoundException">
    /// Thrown if the event log does not exist
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown if the calling user does not have enough privileges to access
    /// the log. Usually that means: admin elevation required.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// When the zone is readonly.
    /// </exception>
    public int UpdateDb1(int cap = Int32.MaxValue)
    {
      using(var db = OpenInnerDatabase1(true))
      {
        return UpdateDb1(db, cap);
      }
    }

    /// <summary>
    /// Insert missing records into the V2 database from the event log, taking into
    /// account the filter settings. This overload opens the inner db, performs
    /// the update, then closes it again.
    /// Also updates touch tag files in the DB folder.
    /// </summary>
    /// <param name="cap">
    /// The maximum number of records to insert
    /// </param>
    /// <returns>
    /// The number of records inserted.
    /// </returns>
    /// <exception cref="EventLogNotFoundException">
    /// Thrown if the event log does not exist
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown if the calling user does not have enough privileges to access
    /// the log. Usually that means: admin elevation required.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// When the zone is readonly.
    /// </exception>
    public int UpdateDb2(int cap = Int32.MaxValue)
    {
      int updateCount;
      EventHeaderRow? lastEvent;
      // Cannot use OpenInnerDatabase because we still need the outer database afterward.
      var redb = OpenDatabase2(true);
      using(var db = redb.Open(true))
      {
        updateCount = UpdateDb2(db, cap);
        var lastRid = db.MaxRecordId();
        lastEvent = lastRid.HasValue ? db.FindEventHeader(lastRid.Value) : null;
      }
      // update tag files
      var dir = redb.DbDirectory;

      var dbUpdatedTagFile = Path.Combine(dir, "db-updated.tag");
      TouchFile(dbUpdatedTagFile);
      var dbStamp = File.GetLastWriteTimeUtc(redb.FileName);
      File.SetLastWriteTimeUtc(dbUpdatedTagFile, dbStamp);

      var lastEventFile = Path.Combine(dir, "last-event.rid.txt");
      if(lastEvent != null)
      {
        File.WriteAllText(lastEventFile, lastEvent.RecordId.ToString());
        var eventStamp = lastEvent.Stamp.EpochDateTime();
        File.SetLastWriteTimeUtc(lastEventFile, eventStamp);
      }
      
      var dbUpdateRunTagFile = Path.Combine(dir, "db-update-run.tag");
      TouchFile(dbUpdateRunTagFile);
      var now = DateTime.UtcNow;
      File.SetLastWriteTimeUtc(dbUpdateRunTagFile, now);
      
      return updateCount;
    }

    private static void TouchFile(string fileName)
    {
      if(!File.Exists(fileName))
      {
        using(var _ = File.Create(fileName))
        {
        }
      }
    }

    /// <summary>
    /// Insert missing records into the database from the event log, taking into
    /// account the filter settings. This overload takes an already opened
    /// inner db as argument.
    /// </summary>
    /// <param name="db">
    /// The already-opened inner database (for instance opened with
    /// <see cref="OpenInnerDatabase1(bool)"/>)
    /// </param>
    /// <param name="cap">
    /// The maximum number of records to insert
    /// </param>
    /// <returns>
    /// The number of records inserted.
    /// </returns>
    /// <exception cref="EventLogNotFoundException">
    /// Thrown if the event log does not exist
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown if the calling user does not have enough privileges to access
    /// the log. Usually that means: admin elevation required.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// When the zone is readonly.
    /// </exception>
    public int UpdateDb1(RawEventDbV1.OpenDb db, int cap = Int32.MaxValue)
    {
      return db.UpdateFrom(Configuration.Channel, cap);
    }

    /// <summary>
    /// Insert missing records into the V2 database from the event log, taking into
    /// account the filter settings. This overload takes an already opened
    /// inner db as argument.
    /// </summary>
    /// <param name="db">
    /// The already-opened inner database (for instance opened with
    /// <see cref="OpenInnerDatabase1(bool)"/>)
    /// </param>
    /// <param name="cap">
    /// The maximum number of records to insert
    /// </param>
    /// <returns>
    /// The number of records inserted.
    /// </returns>
    /// <exception cref="EventLogNotFoundException">
    /// Thrown if the event log does not exist
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown if the calling user does not have enough privileges to access
    /// the log. Usually that means: admin elevation required.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// When the zone is readonly.
    /// </exception>
    public int UpdateDb2(OpenDbV2 db, int cap = Int32.MaxValue)
    {
      return db.UpdateFrom(Configuration.Channel, cap);
    }

    /// <summary>
    /// Return the maximum imported record ID for this job's channel,
    /// or 0 if no records were imported or the database has not been
    /// initialized yet.
    /// </summary>
    public long MaxRecordId1()
    {
      if(!HasDbV1)
      {
        return 0L;
      }
      using(var db = OpenInnerDatabase1(false))
      {
        return db.MaxRecordId() ?? 0L;
      }
    }

    /// <summary>
    /// Return the minimum imported record ID for this job's channel,
    /// or 0 if no records were imported or the database has not been
    /// initialized yet.
    /// </summary>
    public long MinRecordId1()
    {
      if(!HasDbV1)
      {
        return 0L;
      }
      using(var db = OpenInnerDatabase1(false))
      {
        return db.MinRecordId() ?? 0L;
      }
    }

    /// <summary>
    /// Return the maximum imported record ID for this job's channel,
    /// or 0 if no records were imported or the database has not been
    /// initialized yet.
    /// </summary>
    public long MaxRecordId2()
    {
      if(!HasDbV2)
      {
        return 0L;
      }
      using(var db = OpenInnerDatabase2(false))
      {
        return db.MaxRecordId() ?? 0L;
      }
    }

    /// <summary>
    /// Return the minimum imported record ID for this job's channel,
    /// or 0 if no records were imported or the database has not been
    /// initialized yet.
    /// </summary>
    public long MinRecordId2()
    {
      if(!HasDbV2)
      {
        return 0L;
      }
      using(var db = OpenInnerDatabase2(false))
      {
        return db.MinRecordId() ?? 0L;
      }
    }

    /// <summary>
    /// Get an overview of the legacy data: a list of DbOverview1 records, 
    /// one per unique (event ID, task ID) combination (usually that
    /// means one row per event ID)
    /// </summary>
    public IReadOnlyList<DbOverview1> GetOverview1(bool includeSize)
    {
      if(!HasDbV1)
      {
        return Array.Empty<DbOverview1>();
      }
      using(var db = OpenInnerDatabase1(false))
      {
        return includeSize ? db.GetOverview() : db.GetOverviewWithoutSize();
      }
    }

    /// <summary>
    /// Get an overview of the (v2) data.
    /// </summary>
    /// <param name="includeCounts">
    /// Include event counts (big performance hit!)
    /// </param>
    public IReadOnlyList<DbOverviewRow> GetOverview2(bool includeCounts)
    {
      if(!HasDbV2)
      {
        return Array.Empty<DbOverviewRow>();
      }
      using(var db = OpenInnerDatabase2(false))
      {
        return db.GetOverview(includeCounts).ToList().AsReadOnly();
      }
    }

    /// <summary>
    /// Open the inner V1 database. Make sure to Dispose() it after use.
    /// </summary>
    public RawEventDbV1.OpenDb OpenInnerDatabase1(bool writable)
    {
      var redb = OpenDatabase1(writable);
      return redb.Open(writable);
    }

    /// <summary>
    /// Open the inner V2 database. Make sure to Dispose() it after use.
    /// </summary>
    public OpenDbV2 OpenInnerDatabase2(bool writable)
    {
      var redb = OpenDatabase2(writable);
      return redb.Open(writable);
    }

    /// <summary>
    /// Open the job's database file, creating it if it did not yet exist.
    /// To operate on the database use the Open() method of the returned
    /// RawEventDb object, or use <see cref="OpenInnerDatabase1"/> to skip
    /// the middle man.
    /// </summary>
    /// <param name="writable">
    /// Whether to open the DB read-only or writable.
    /// False also prevents database creation
    /// </param>
    /// <returns>
    /// The database interface object.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// When passing <paramref name="writable"/> = true when the zone is readonly.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// When the DB did not exist yet and <paramref name="writable"/> = false.
    /// </exception>
    public RawEventDbV1 OpenDatabase1(bool writable)
    {
      if(writable && Zone.ReadOnly)
      {
        throw new InvalidOperationException(
          $"Cannot open a writable DB in a read-only zone");
      }
      if(!HasDbV1)
      {
        if(writable)
        {
          InitDb();
        }
        else
        {
          throw new FileNotFoundException(
            $"Cannot open a non-existing database as read-only",
            RawDbFileV1);
        }
      }
      return new RawEventDbV1(RawDbFileV1, writable, false);
    }

    /// <summary>
    /// Open the job's V2 database file, creating it if it did not yet exist.
    /// To operate on the database use the Open() method of the returned
    /// RawEventDb object, or use <see cref="OpenInnerDatabase2"/> to skip
    /// the middle man.
    /// </summary>
    /// <param name="writable">
    /// Whether to open the DB read-only or writable.
    /// False also prevents database creation
    /// </param>
    /// <returns>
    /// The database interface object.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// When passing <paramref name="writable"/> = true when the zone is readonly.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// When the DB did not exist yet and <paramref name="writable"/> = false.
    /// </exception>
    public RawEventDbV2 OpenDatabase2(bool writable)
    {
      if(writable && Zone.ReadOnly)
      {
        throw new InvalidOperationException(
          $"Cannot open a writable DB in a read-only zone");
      }
      if(!HasDbV2)
      {
        if(writable)
        {
          InitDb();
        }
        else
        {
          throw new FileNotFoundException(
            $"Cannot open a non-existing database as read-only",
            RawDbFileV2);
        }
      }
      return new RawEventDbV2(RawDbFileV2, writable, false);
    }

    /// <summary>
    /// If the db does not exist yet, create it as an empty db.
    /// Currently creates BOTH the new and legacy versions.
    /// </summary>
    public void InitDb()
    {
      if(!HasDbV1)
      {
        if(Zone.ReadOnly)
        {
          throw new InvalidOperationException(
            "Cannot initialize DB: the data zone is read-only");
        }
        else
        {
          InitFolder();
          var redb1 = new RawEventDbV1(RawDbFileV1, true, true);
          using(var db1 = redb1.Open(true, true))
          {
            db1.DbInit();
          }
        }
      }
      if(!HasDbV2)
      {
        if(Zone.ReadOnly)
        {
          throw new InvalidOperationException(
            "Cannot initialize DB: the data zone is read-only");
        }
        else
        {
          InitFolder();
          var redb2 = new RawEventDbV2(RawDbFileV2, true, true);
          using(var db2 = redb2.Open(true, true))
          {
            db2.DbInit();
          }
        }
      }
    }

    /// <summary>
    /// Upgrade the V2 DB, adding some new DB objects if missing
    /// </summary>
    public bool UpgradeDb()
    {
      if(Zone.ReadOnly)
      {
        throw new InvalidOperationException(
          "Cannot upgrade DB: the data zone is read-only");
      }
      if(!HasDbV2)
      {
        InitFolder();
      }
      // The following creates the DB if needed, and creates missing DB objects
      var redb2 = new RawEventDbV2(RawDbFileV2, true, true);
      using(var db2 = redb2.Open(true, true))
      {
        return db2.DbInit(); // this now includes the upgrade
      }
    }

    private void InitFolder()
    {
      if(!Directory.Exists(JobFolder))
      {
        Directory.CreateDirectory(JobFolder);
      }
    }

  }
}
