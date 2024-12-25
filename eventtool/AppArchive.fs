module AppArchive

open System
open System.Globalization
open System.IO

open Newtonsoft.Json

open Lcl.EventLog.Jobs
open Lcl.EventLog.Jobs.Archival
open Lcl.EventLog.Jobs.Database
open Lcl.EventLog.Jobs.Database.Skeleton
open Lcl.EventLog.Utilities
open Lcl.EventLog.Utilities.Xml

open ColorPrint
open CommonTools

type private ListOptions = {
  JobName: string
  MachineName: string
}

let private runListInner o =
  let ezc = new EventZoneConfig(o.MachineName)
  let edz = new EventDataZone(true, ezc.Machine)
  if edz.Exists |> not then
    cp $"\frNo data for machine \f0'\fc{ezc.Machine}\f0'"
    1
  else
    let job = edz.TryOpenJob(o.JobName)
    if job = null then
      cp $"\frNo job or channel \f0'\fg{o.JobName}\f0' \frknown for machine \f0'\fc{o.MachineName}\f0'"
      1
    else
      let archives =
        job.EnumArchives()
        |> Seq.toArray
      if archives.Length = 0 then
        cp $"\foNo archive file found for job \f0'\fg{o.JobName}\f0'"
        0
      else
        cp $"Found \fb{archives.Length}\f0 archive files\f0:"
        for archive in archives do
          let ridMinTxt =
            if archive.RidMin.HasValue then
              $"\fy{archive.RidMin.Value:D6}"
            else
              "\fr???"
          let ridMaxTxt =
            if archive.RidMax.HasValue then
              $"\fy{archive.RidMax.Value:D6}"
            else
              "\fr???"
          let compressText =
            if archive.Compressed then "(\fgcompressed\f0)" else "(\fRuncompressed\f0)"
          cp $"\fg{archive.JobName} \fc{archive.MonthTag} {ridMinTxt}\f0 - {ridMaxTxt}\f0 {compressText}"
        0

let private runList args =
  let rec parsemore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parsemore o
    | "-h" :: _ ->
      None
    | "-job" :: job :: rest ->
      if job |> EventJobConfig.IsValidJobName |> not then
        cp $"'\fg{job}\f0' \fois not a valid job name\f0."
        None
      else
        rest |> parsemore {o with JobName = job}
    | "-m" :: mnm :: rest
    | "-machine" :: mnm :: rest ->
      rest |> parsemore {o with MachineName = mnm}
    | [] ->
      if o.JobName |> String.IsNullOrEmpty then
        cp "\foNo job name provided\f0."
        None
      else
        o |> Some
    | x :: _ ->
      cp $"\foUnrecognized argument \f0'\fy{x}\f0'"
      None
  let oo = args |> parsemore {
    JobName = null
    MachineName = Environment.MachineName
  }
  match oo with
  | Some(o) ->
    o |> runListInner
  | None ->
    Usage.usage "archive"
    1

type private BuildOptions = {
  JobName: string
  MachineName: string
  Dry: bool
  RidStart: int64 option
  Repeat: bool
}

let private runBuildInner o =
  let ezc = new EventZoneConfig(o.MachineName)
  let edz = new EventDataZone(true, ezc.Machine)
  if edz.Exists |> not then
    cp $"\frNo data for machine \f0'\fc{ezc.Machine}\f0'"
    1
  else
    let job = edz.TryOpenJob(o.JobName)
    if job = null then
      cp $"\frNo job or channel \f0'\fg{o.JobName}\f0' \frknown for machine \f0'\fc{o.MachineName}\f0'"
      1
    else
      let ridStart =
        match o.RidStart with
        | Some(rid) ->
          rid
        | None ->
          let archives =
            job.EnumArchives()
            |> Seq.toArray
          // If only Seq.tryMax existed :(
          let maxRids =
            archives
            |> Seq.choose (fun ai -> ai.RidMax |> Option.ofNullable)
            |> Seq.toArray
          if maxRids.Length = 0 then
            1L
          else
            let maxRid = maxRids |> Seq.max
            maxRid + 1L
      let archiveBuilder = new ArchiveBuilder(job, ridStart)
      let errorMessage = archiveBuilder.Validate(false)
      if errorMessage |> String.IsNullOrEmpty |> not then
        if errorMessage.Contains("partial archive") then
          cp $"\foStopping: \fy{errorMessage}\f0."
          2
        else
          cp $"\foRequest validation failed: \fy{errorMessage}\f0."
          1
      else
        let fileName = archiveBuilder.GetArchiveFile(true)
        let shortName = fileName |> Path.GetFileName
        let eventCount = archiveBuilder.RidEnd.Value - archiveBuilder.RidStart + 1L
        cp $"Archive file: \fg{shortName}\f0. \fb{eventCount}\f0 events."
        if o.Dry |> not then
          archiveBuilder.Build(true, false)
          0
        else
          cp "\fo-dry\f0 was specified. Not running actual archiving"
          0

let private runBuildRepeat o =
  let mutable status = 0
  cp "\fyPress \fRCTRL-C\fy to abort after the current archive\f0."
  while status = 0 && (canceled() |> not) do
    status <- o |> runBuildInner
  if status = 2 then 0 elif status = 0 then 3 (*canceled*) else status

let private runBuild args =
  let rec parsemore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parsemore o
    | "-h" :: _ ->
      None
    | "-dry" :: rest ->
      rest |> parsemore {o with Dry = true}
    | "-rid" :: ridTxt :: rest ->
      let ok, rid = ridTxt |> Int64.TryParse
      if ok && rid >= 1L then
        rest |> parsemore {o with RidStart = rid |> Some}
      else
        cp "\foExpecting a positive number after \fg-rid\f0."
        None
    | "-job" :: job :: rest ->
      if job |> EventJobConfig.IsValidJobName |> not then
        cp $"'\fg{job}\f0' \fois not a valid job name\f0."
        None
      else
        rest |> parsemore {o with JobName = job}
    | "-m" :: mnm :: rest
    | "-machine" :: mnm :: rest ->
      rest |> parsemore {o with MachineName = mnm}
    | "-repeat" :: rest ->
      rest |> parsemore {o with Repeat = true}
    | [] ->
      if o.JobName |> String.IsNullOrEmpty then
        cp "\foNo job name provided\f0."
        None
      else
        o |> Some
    | x :: _ ->
      cp $"\foUnrecognized argument \f0'\fy{x}\f0'"
      None
  let oo = args |> parsemore {
    JobName = null
    MachineName = Environment.MachineName
    Dry = false
    RidStart = None
    Repeat = false
  }
  match oo with
  | Some(o) ->
    if o.Repeat then
      o |> runBuildRepeat
    else
      o |> runBuildInner
  | None ->
    Usage.usage "archive"
    1

type private PurgeOptions = {
  Before: DateTime option
  JobName: string
  MachineName: string
  Dry: bool
  Cap: int
}

let private runPurgeInner o =
  let before = o.Before.Value
  let ezc = new EventZoneConfig(o.MachineName)
  let edz = new EventDataZone(o.Dry, ezc.Machine)
  if edz.Exists |> not then
    cp $"\frNo data for machine \f0'\fc{ezc.Machine}\f0'"
    1
  else
    let job = edz.TryOpenJob(o.JobName)
    if job = null then
      cp $"\frNo job or channel \f0'\fg{o.JobName}\f0' \frknown for machine \f0'\fc{o.MachineName}\f0'"
      1
    else
      let ridMin = job.MinRecordId2()
      let archives =
        job.EnumArchives()
        |> Seq.where (fun ai ->
            ai.IsSealed &&
            ai.Compressed &&
            ai.UtcStart <= before &&
            ai.RidMin.Value >= ridMin)
        |> Seq.truncate o.Cap
        |> Seq.toArray
      if archives.Length = 0 then
        let beforeText = before.ToString("yyyy-MM-dd")
        cp $"\foNo (compressed) archive files to purge found for job \f0'\fg{o.JobName}\f0' (before {beforeText})"
        1
      else
        let dryText = if o.Dry then " \f0(\fyDry run\f0)" else ""
        cp $"Processing \fb{archives.Length}\f0 matching archive files\f0{dryText}"
        if archives.Length = o.Cap then
          cp $"(\fycapped at \fb{o.Cap}\f0)"
        use odb = job.OpenInnerDatabase2(o.Dry |> not)
        let mutable total = 0
        let tAfter = TimeUtil.TicksSinceEpoch(before)
        for archive in archives do
          let ridStart = archive.RidMin.Value
          let ridBefore = // the EXCLUSIVE end
            if archive.UtcAfter > before then
              let stopRecord =
                odb.QueryEventHeaders(archive.RidMin, archive.RidMax, tMin = tAfter, limit=1)
                |> Seq.tryHead
              // if none found: all existing records in the archive are available for deletion
              match stopRecord with
              | None ->
                archive.RidMax.Value + 1L
              | Some(r) ->
                r.RecordId
            else // no need to do an expensive search
              archive.RidMax.Value + 1L
          let fnm = archive.GetSealedName() |> Path.GetFileName
          if ridStart < ridBefore then
            cp $"Processing \fg{fnm}\f0: [\fb{ridStart:D8}\f0, \fb{ridBefore:D8}\f0>{dryText}"
            if o.Dry |> not then
              let deleted = odb.DeleteEvents(ridStart, ridBefore)
              cp $"  Deleted \fr{deleted}\f0 event records"
          else
            cp $"Processing \fg{fnm}\f0: [\fyno elegible events\f0]{dryText}"
        0

let private runPurge args =
  let rec parsemore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parsemore o
    | "-h" :: _ ->
      None
    | "-before" :: txt :: rest ->
      let maxBefore = DateTime.UtcNow.AddMonths(-3).Date
      let ok, before =
        DateTime.TryParseExact(
          txt, "yyyy-MM-dd", CultureInfo.InvariantCulture,
          DateTimeStyles.AssumeUniversal ||| DateTimeStyles.AdjustToUniversal)
      if ok then
        if before > maxBefore then
          cp "\foExpecting a \fg-before\f0 date at least 3 months old"
          None
        else
          rest |> parsemore {o with Before = before |> Some}
      else
        cp $"\foExpecting a date in \fcyyyy-MM-dd\fo format but got \fC{txt}\f0."
        None
    | "-dry" :: rest ->
      rest |> parsemore {o with Dry = true}
    | "-cap" :: captxt :: rest ->
      let ok, cap = captxt |> Int32.TryParse
      if ok && cap > 0 then
        rest |> parsemore {o with Cap = cap}
      else
        cp $"\foExpacting a positive integer as argument to \fG-cap\f0."
        None
    | "-job" :: job :: rest ->
      if job |> EventJobConfig.IsValidJobName |> not then
        cp $"'\fg{job}\f0' \fois not a valid job name\f0."
        None
      else
        rest |> parsemore {o with JobName = job}
    | "-m" :: mnm :: rest
    | "-machine" :: mnm :: rest ->
      rest |> parsemore {o with MachineName = mnm}
    | [] ->
      if o.JobName |> String.IsNullOrEmpty then
        cp "\foNo job name provided\f0."
        None
      elif o.Before.IsNone then
        cp "\foNo \fG-before\fo date provided\f0."
        None
      else
        o |> Some
    | x :: _ ->
      cp $"\foUnrecognized argument \f0'\fy{x}\f0'"
      None
  let oo = args |> parsemore {
    Before = None
    JobName = null
    MachineName = Environment.MachineName
    Dry = false
    Cap = 12
  }
  match oo with
  | None ->
    Usage.usage "archive"
    1
  | Some(o) ->
    o |> runPurgeInner

let run args =
  match args with
  | "list" :: rest ->
    rest |> runList
  | "build" :: rest ->
    rest |> runBuild
  | "purge" :: rest ->
    rest |> runPurge
  | "-h" :: _ ->
    Usage.usage "archive"
    1
  | x :: _ ->
    cp $"\foUnrecognized subcommand \fy{x}\f0."
    Usage.usage "archive"
    1
  | [] ->
    cp $"\foNo subcommand provided after \f0'\fyarchive\f0'"
    Usage.usage "archive"
    1

