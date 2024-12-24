/*
 * (c) 2024  ttelcl / ttelcl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lcl.EventLog.Jobs.Archival;

/// <summary>
/// Description of an event archive file, derived or determining
/// that file's name.
/// </summary>
public class ArchiveInfo
{
  /// <summary>
  /// Create a new ArchiveInfo
  /// </summary>
  public ArchiveInfo(
    string machineName,
    string jobName,
    int year,
    int month,
    long? ridMin,
    long? ridMax,
    bool compressed = true)
  {
    MachineName = String.IsNullOrEmpty(machineName) ? Environment.MachineName : machineName;
    if(!EventJobConfig.IsValidJobName(jobName))
    {
      throw new ArgumentException(
        $"Invalid job name: {jobName}",
        nameof(jobName));
    }
    JobName = jobName;
    if(year < 2000 || year > 2099)
    {
      throw new ArgumentOutOfRangeException(
        nameof(year),
        year,
        "Year must be in the range 2000-2099");
    }
    Year = year;
    if(month < 1 || month > 12)
    {
      throw new ArgumentOutOfRangeException(
        nameof(month),
        month,
        "Month must be in the range 01-12");
    }
    Month = month;
    if(ridMin.HasValue && ridMin < 1)
    {
      ridMin = null;
    }
    if(ridMax.HasValue && ridMax < 1)
    {
      ridMax = null;
    }
    if(ridMin.HasValue && ridMax.HasValue && ridMin > ridMax)
    {
      throw new ArgumentException(
        "ridMin must be less than or equal to ridMax",
        nameof(ridMin));
    }
    RidMin = ridMin;
    RidMax = ridMax;
    Compressed = compressed;
  }

  /// <summary>
  /// The name of the computer providing the events archived in this file
  /// </summary>
  public string MachineName { get; }

  /// <summary>
  /// The name of the associated job (implying both the database and the log channel)
  /// </summary>
  public string JobName { get; }

  /// <summary>
  /// The year of the month for which the archive contains data
  /// </summary>
  public int Year { get; }

  /// <summary>
  /// The month for which the archive contains data (in year <see cref="Year"/>)
  /// </summary>
  public int Month { get; }

  /// <summary>
  /// A string combining <see cref="Year"/> and <see cref="Month"/>
  /// </summary>
  public string MonthTag { get => $"{Year:D4}-{Month:D2}"; }

  /// <summary>
  /// The minimum record ID in the archive, or null if not yet known
  /// </summary>
  public long? RidMin { get; private set; }

  /// <summary>
  /// The maximum record ID in the archive, or null if not yet known
  /// </summary>
  public long? RidMax { get; private set; }

  /// <summary>
  /// True if both <see cref="RidMin"/> and <see cref="RidMax"/> are set,
  /// implying that the archive file should be treated as read-only.
  /// </summary>
  public bool IsSealed { get => RidMin.HasValue && RidMax.HasValue; }

  /// <summary>
  /// Default state of the archive file: compressed or uncompressed.
  /// This just provides a default for the file name if not explicitly
  /// passed to <see cref="GetUnsealedName"/> or <see cref="GetSealedName"/>.
  /// </summary>
  public bool Compressed { get; set; }

  /// <summary>
  /// Get the name of the archive file if it were not sealed (even if it is).
  /// </summary>
  public string GetUnsealedName(bool? compressed = null)
  {
    var suffix = (compressed ?? Compressed) ? ".gz" : "";
    return $"{MachineName}.{JobName}.archive.{MonthTag}.-.evarc" + suffix;
  }

  /// <summary>
  /// Get the name of the sealed archive file. Throws an exception if the
  /// archive is not sealed.
  /// </summary>
  public string GetSealedName(bool? compressed = null)
  {
    if(!RidMin.HasValue || !RidMax.HasValue)
    {
      throw new InvalidOperationException(
        "Expecting a sealed archive");
    }
    var suffix = (compressed ?? Compressed) ? ".gz" : "";
    return $"{MachineName}.{JobName}.archive.{MonthTag}.{RidMin.Value:D6}-{RidMax.Value:D6}.evarc" + suffix;
  }

  /// <summary>
  /// Seal the archive. If the archive is already fully or partially sealed,
  /// the given range must be compatible with the existing range.
  /// </summary>
  /// <param name="ridMin">
  /// The lower bound of the record ID of the events in the archive.
  /// </param>
  /// <param name="ridMax">
  /// The upper bound of the record ID of the events in the archive.
  /// </param>
  public void Seal(long ridMin, long ridMax)
  {
    if(ridMin < 1 || ridMax < 1 || ridMin > ridMax)
    {
      throw new ArgumentException(
        "Invalid record ID range",
        nameof(ridMin));
    }
    if((RidMin.HasValue && RidMin.Value!=ridMin)
      || (RidMax.HasValue && RidMax.Value!=ridMax))
    {
      throw new InvalidOperationException(
        "Archive is already sealed, or conflicting values supplied");
    }
    RidMin = ridMin;
    RidMax = ridMax;
  }

  /// <summary>
  /// Parse the components of an archive file name and return an ArchiveInfo
  /// </summary>
  /// <param name="fileName">
  /// The file name to parse (if it includes a path, that will be ignored)
  /// </param>
  public static ArchiveInfo FromFileName(string fileName)
  {
    fileName = Path.GetFileName(fileName);
    var compressed = fileName.EndsWith(".gz", StringComparison.InvariantCultureIgnoreCase);
    if(compressed)
    {
      fileName = fileName.Substring(0, fileName.Length-3);
    }
    var parts = fileName.Split('.');
    if(parts.Length != 6)
    {
      throw new ArgumentException(
        "Expecting file name to have 6 parts (ignoring the optional '.gz' at the end)",
        nameof(fileName));
    }
    if(!parts[2].Equals("archive", StringComparison.InvariantCultureIgnoreCase))
    {
      throw new ArgumentException(
        "Expecting file name to start with 'archive'",
        nameof(fileName));
    }
    if(!parts[5].Equals(".evarc", StringComparison.InvariantCultureIgnoreCase))
    {
      throw new ArgumentException(
        "Expecting file name to end with '.evarc' or '.evarc.gz'",
        nameof(fileName));
    }
    var machineName = parts[0];
    var jobName = parts[1];
    var monthTag = parts[3];
    var ridParts = parts[4].Split('-');
    if(ridParts.Length != 2)
    {
      throw new ArgumentException(
        "Expecting record ID range part of the file name to be separated by a hyphen",
        nameof(fileName));
    }
    long? ridMin = String.IsNullOrEmpty(ridParts[0]) ? null : Int64.Parse(ridParts[0]);
    long? ridMax = String.IsNullOrEmpty(ridParts[1]) ? null : Int64.Parse(ridParts[1]);
    if(!Regex.IsMatch(monthTag, @"^\d{4}-\d{2}$"))
    {
      throw new ArgumentException(
        "Expecting time tag part of the file name to be in the form 'yyyy-MM'",
        nameof(fileName));
    }
    return new ArchiveInfo(
      machineName,
      jobName,
      Int32.Parse(monthTag[..4]),
      Int32.Parse(monthTag[5..7]),
      ridMin,
      ridMax,
      compressed);
  }

  /// <summary>
  /// Enumerate all archive files in the given folder that match the given job name.
  /// See also <see cref="EventJob.EnumArchives(string?)"/> for a more compact
  /// way to call the most common use case.
  /// </summary>
  /// <param name="folderName">
  /// The folder to search for archive files.
  /// </param>
  /// <param name="jobName">
  /// The job name to match.
  /// </param>
  /// <param name="machineName">
  /// The machine name (defaults to the local machine name).
  /// </param>
  /// <returns></returns>
  public static IEnumerable<ArchiveInfo> FindArchives(
    string folderName, string jobName, string? machineName = null)
  {
    machineName ??= Environment.MachineName;
    if(!EventJobConfig.IsValidJobName(jobName))
    {
      throw new ArgumentException(
        $"Invalid job name: {jobName}",
        nameof(jobName));
    }
    var prefix = $"{machineName}.{jobName}.archive.";
    var folder = new DirectoryInfo(folderName);
    var candidateFiles1 =
      folder
      .GetFiles(prefix + "*.evarc")
      .Where(f => f.Name.Split('.').Length == 6)
      .ToList();
    var candidateFiles2 =
      folder
      .GetFiles(prefix + "*.evarc.gz")
      .Where(f => f.Name.Split('.').Length == 7)
      .ToList();
    var files = new List<ArchiveInfo>();
    foreach(var file in candidateFiles1.Concat(candidateFiles2))
    {
      try
      {
        files.Add(FromFileName(file.Name));
      }
      catch(Exception ex)
      {
        Trace.TraceError(
          $"Error parsing archive file '{file.Name}' (ignoring): {ex.Message}");
      }
    }
    return files;
  }
}
