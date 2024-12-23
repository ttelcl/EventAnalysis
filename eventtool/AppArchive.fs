module AppArchive

open System

open Newtonsoft.Json

open Lcl.EventLog.Jobs
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
          cp $"\fg{archive.JobName} \fc{archive.MonthTag} {ridMinTxt}\f0 - {ridMaxTxt}\f0."
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

let run args =
  match args with
  | "list" :: rest ->
    rest |> runList
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

