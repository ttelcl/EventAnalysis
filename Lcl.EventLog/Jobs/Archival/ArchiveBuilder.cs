/*
 * (c) 2024  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Lcl.EventLog.Jobs.Database;
using Lcl.EventLog.Utilities;

namespace Lcl.EventLog.Jobs.Archival;

/// <summary>
/// Description of ArchiveBuilder
/// </summary>
public class ArchiveBuilder
{
  /// <summary>
  /// Create a new ArchiveBuilder
  /// </summary>
  /// <param name="owner">
  /// The event job describing the database and the event source to create
  /// an archive for.
  /// </param>
  /// <param name="ridStart">
  /// The starting record ID to archive. If this record ID does not exist,
  /// the next valid one is used instead.
  /// </param>
  /// <param name="ridEnd">
  /// The final record ID to archive, or null to have this class determine
  /// the end.
  /// </param>
  public ArchiveBuilder(
    EventJob owner,
    long ridStart,
    long? ridEnd = null)
  {
    Owner = owner;
    RidStart = ridStart;
    RidEnd = ridEnd;
  }

  /// <summary>
  /// Get the owning event job
  /// </summary>
  public EventJob Owner { get; }

  /// <summary>
  /// Get the starting record ID
  /// </summary>
  public long RidStart { get; private set; }

  /// <summary>
  /// Get the ending record ID, or null if the end is not yet determined
  /// </summary>
  public long? RidEnd { get; private set; }

  /// <summary>
  /// True after <see cref="Validate"/> has been called.
  /// See <see cref="IsValid"/> for the result.
  /// </summary>
  public bool HasBeenValidated { get; private set; }

  /// <summary>
  /// True if <see cref="Validate"/> has been called and the
  /// parameters are valid, and the archive can be built.
  /// </summary>
  public bool IsValid { get; private set; }

  /// <summary>
  /// The Archive Info for the archive to be built; calculated
  /// toward the end of <see cref="Validate"/> if successful.
  /// Initially its <see cref="ArchiveInfo.Compressed"/> will
  /// be true, but this can be changed.
  /// </summary>
  public ArchiveInfo? ArchiveInfo { get; private set; }

  /// <summary>
  /// Get the file name of the archive file, or null if this
  /// builder has not been validated.
  /// </summary>
  /// <param name="compressed">
  /// True (default) to get the file name for the compressed archive,
  /// false to get the file name for the uncompressed archive.
  /// </param>
  /// <returns></returns>
  public string? GetArchiveFile(bool compressed = true)
  {
    if(ArchiveInfo==null)
    {
      return null;
    }
    var shortName = ArchiveInfo.GetSealedName(compressed);
    return Path.Combine(Owner.JobFolder, shortName);
  }

  /// <summary>
  /// Validate the parameters and set <see cref="IsValid"/> and
  /// <see cref="HasBeenValidated"/> accordingly. This will open the
  /// database and check the record IDs, and if necessary, determine
  /// <see cref="RidEnd"/>.
  /// </summary>
  /// <returns>
  /// Returns null on success, or an error message on failure.
  /// </returns>
  public string? Validate()
  {
    if(HasBeenValidated)
    {
      return IsValid ? null : "Earlier validation already failed.";
    }
    HasBeenValidated = true;
    using var odb = Owner.OpenInnerDatabase2(false);
    var firstRecord =
      odb.QueryEventHeaders(RidStart, RidEnd, reverse: false, limit: 1)
      .FirstOrDefault();
    if(firstRecord == null)
    {
      return "No records found at or after the starting record ID";
    }
    RidStart = firstRecord.RecordId;
    var startStamp = TimeUtil.EpochDateTime(firstRecord.Stamp);
    var startYear = startStamp.Year;
    var startMonth = startStamp.Month;
    EventHeaderRow tailRecord;
    if(RidEnd == null)
    {
      var endYear = (startMonth==12) ? startYear+1 : startYear;
      var endMonth = (startMonth==12) ? 1 : startMonth+1;
      var endTime = new DateTime(endYear, endMonth, 1, 0, 0, 0, DateTimeKind.Utc);
      var endStamp = TimeUtil.TicksSinceEpoch(endTime);
      // The first record at or after endStamp is the first record ID that is not
      // valid anymore, determining where the *next* archive will start.
      // Because stamps are not guaranteed to be unique, the end of the current
      // archive is only defined relative to this.
      var nextMonthRecord =
        odb.QueryEventHeaders(RidStart, null, tMin: endStamp, reverse: false, limit: 1)
        .FirstOrDefault();
      if(nextMonthRecord == null)
      {
        // No records for the next month, so the current archive ends at the last DB record
        tailRecord =
          odb.QueryEventHeaders(RidStart, null, reverse: true, limit: 1)
          .First(); // There will be at least one record. We know the one at RidStart exists.
      }
      else
      {
        tailRecord =
          odb.QueryEventHeaders(RidStart, nextMonthRecord.RecordId-1, reverse: true, limit: 1)
          .First(); // There will be at least one record. We know the one at RidStart exists.
      }
    }
    else
    {
      if(RidEnd.Value < RidStart)
      {
        return $"Ending record ID {RidEnd} is less than the starting record ID {RidStart}";
      }
      var lastRecord =
        odb.QueryEventHeaders(RidStart, RidEnd.Value, reverse: true, limit: 1)
        .FirstOrDefault();
      if(lastRecord == null)
      {
        return "No records found at or before the given ending record ID";
      }
      tailRecord = lastRecord;
      RidEnd = tailRecord.RecordId;
    }
    var tailStamp = TimeUtil.EpochDateTime(tailRecord.Stamp);
    if(tailStamp.Year != startYear || tailStamp.Month != startMonth)
    {
      // This may happen if RidEnd was specified.
      return
        $"Ending record ID {RidEnd} is not in the same month as the starting record ID {RidStart}";
    }
    RidEnd = tailRecord.RecordId;

    ArchiveInfo = new ArchiveInfo(
      Owner.Zone.Machine, Owner.Configuration.Name,
      startYear, startMonth, RidStart, RidEnd,
      compressed: true);
    var archFileName = GetArchiveFile(false);
    if(File.Exists(archFileName))
    {
      return $"Archive file {archFileName} already exists";
    }
    archFileName += ".gz";
    if(File.Exists(archFileName))
    {
      return $"Archive file {archFileName} already exists";
    }

    IsValid = true;
    return null;
  }

  public void Build()
  {
    if(!HasBeenValidated)
    {
      throw new InvalidOperationException(
        "Missing call to Validate()");
    }
    if(!IsValid)
    {
      throw new InvalidOperationException(
        "Archive building command is not valid (validation failed)");
    }
    throw new NotImplementedException();
  }

}
