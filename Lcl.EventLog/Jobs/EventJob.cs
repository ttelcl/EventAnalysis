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
    public int UpdateDb(int cap = Int32.MaxValue)
    {
      using(var db = OpenInnerDatabase(true))
      {
        return UpdateDb(db, cap);
      }
    }

    /// <summary>
    /// Insert missing records into the database from the event log, taking into
    /// account the filter settings. This overload takes an already opened
    /// inner db as argument.
    /// </summary>
    /// <param name="db">
    /// The already-opened inner database (for instance opened with
    /// <see cref="OpenInnerDatabase(bool)"/>)
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
    public int UpdateDb(RawEventDbV1.OpenDb db, int cap = Int32.MaxValue)
    {
      return db.UpdateFrom(Configuration.Channel, cap);
    }

    /// <summary>
    /// Return the maximum imported record ID for this job's channel,
    /// or 0 if no records were imported or the database has not been
    /// initialized yet.
    /// </summary>
    public long MaxRecordId()
    {
      if(!HasDbV1)
      {
        return 0L;
      }
      using(var db = OpenInnerDatabase(false))
      {
        return db.MaxRecordId() ?? 0L;
      }
    }

    /// <summary>
    /// Return the minimum imported record ID for this job's channel,
    /// or 0 if no records were imported or the database has not been
    /// initialized yet.
    /// </summary>
    public long MinRecordId()
    {
      if(!HasDbV1)
      {
        return 0L;
      }
      using(var db = OpenInnerDatabase(false))
      {
        return db.MinRecordId() ?? 0L;
      }
    }

    /// <summary>
    /// Get an overview of the data: a list of DbOverview records, 
    /// one per unique (event ID, task ID) combination (usually that
    /// means one row per event ID)
    /// </summary>
    public IReadOnlyList<DbOverview> GetOverview(bool includeSize)
    {
      if(!HasDbV1)
      {
        return Array.Empty<DbOverview>();
      }
      using(var db = OpenInnerDatabase(false))
      {
        return includeSize ? db.GetOverview() : db.GetOverviewWithoutSize();
      }
    }

    /// <summary>
    /// Open the inner database. Make sure to Dispose() it after use.
    /// </summary>
    public RawEventDbV1.OpenDb OpenInnerDatabase(bool writable)
    {
      var redb = OpenDatabase(writable);
      return redb.Open(writable);
    }

    /// <summary>
    /// Open the job's database file, creating it if it did not yet exist.
    /// To operate on the database use the Open() method of the returned
    /// RawEventDb object, or use <see cref="OpenInnerDatabase"/> to skip
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
    public RawEventDbV1 OpenDatabase(bool writable)
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
    /// If the db does not exist yet, create it as an empty db.
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
          var redb = new RawEventDbV1(RawDbFileV1, true, true);
          using(var db = redb.Open(true, true))
          {
            db.DbInit();
          }
        }
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
