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

type private ArchiveMode =
  | Copy
  | Move

type private ArchiveRange =
  | Days of int
  | Records of int

type private Options = {
  JobName: string
  MachineName: string
  Mode: ArchiveMode option
  Range: ArchiveRange option
}

let private runApp o =
  let ezc = new EventZoneConfig(o.MachineName)
  let edz = new EventDataZone(false, ezc.Machine)
  if edz.Exists |> not then
    cp $"\frNo data for machine \f0'\fc{ezc.Machine}\f0'"
    1
  else
    let job = edz.TryOpenJob(o.JobName)
    if job = null then
      cp $"\frNo job or channel \f0'\fg{o.JobName}\f0' \frknown for machine \f0'\fc{o.MachineName}\f0'"
      1
    else
      if job.HasDbV2 |> not then
        cp $"\foWarning\f0: No data recorded yet for job \f0'\fg{o.JobName}\f0'"
      cp "\fr NOT YET IMPLEMENTED\f0."
      1

let run args =
  let rec parsemore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parsemore o
    | "-h" :: _ ->
      None
    | "-job" :: jobname :: rest ->
      rest |> parsemore {o with JobName = jobname}
    | "-machine" :: mnm :: rest
    | "-m" :: mnm :: rest ->
      rest |> parsemore {o with MachineName = mnm}
    | "-copy" :: rest ->
      rest |> parsemore {o with Mode = ArchiveMode.Copy |> Some}
    | "-move" :: rest ->
      cp "\fy -move \fris not yet supported \f0."
      None
      // rest |> parsemore {o with Mode = ArchiveMode.Move |> Some}
    | "-days" :: ntxt :: rest ->
      let ok,n = ntxt |> Int32.TryParse
      if ok then
        if n>0 then
          rest |> parsemore {o with Range = n |> ArchiveRange.Days |> Some}
        else
          cp $"\foInvalid day count \fb{n}\f0."
          None
      else
        cp $"\foExpecting a number after \fG-days\fo but got \fb{ntxt}\f0."
        None
    | "-n" :: ntxt :: rest ->
      let ok,n = ntxt |> Int32.TryParse
      if ok then
        if n>0 then
          rest |> parsemore {o with Range = n |> ArchiveRange.Records |> Some}
        else
          cp $"\foInvalid record count \fb{n}\f0."
          None
      else
        cp $"\foExpecting a number after \fG-n\fo but got \fb{ntxt}\f0."
        None
    | [] ->
      if o.JobName |> String.IsNullOrEmpty then
        cp "\foNo \fg-job\fo name specified\f0"
        None
      elif o.Mode.IsNone then
        cp "\foNo mode (\fg-copy\fo or \fg-move\fo) specified\f0."
        None
      elif o.Range.IsNone then
        cp "\foNo range (\fg-days\fo or \fg-n\fo) specified\f0."
        None
      else
        o |> Some
    | x :: _ ->
      cp $"\foUnrecognized argument \fy{x}\f0."
      None
  let oo = args |> parsemore {
    JobName = null
    MachineName = Environment.MachineName
    Mode = None
    Range = None
  }
  match oo with
  | Some(o) ->
    o |> runApp
  | None ->
    Usage.usage "archive"
    1

