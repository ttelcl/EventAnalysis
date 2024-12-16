module AppMetaDump

open System

open Newtonsoft.Json

open Lcl.EventLog.Jobs
open Lcl.EventLog.Jobs.Database
open Lcl.EventLog.Jobs.Database.Skeleton
open Lcl.EventLog.Utilities
open Lcl.EventLog.Utilities.Xml

open ColorPrint
open CommonTools

type private Options = {
  JobName: string
  MachineName: string
}

let private runApp o =
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
      if job.HasDbV2 |> not then
        cp $"\foWarning\f0: No data recorded yet for job \f0'\fg{o.JobName}\f0'"
      let onm = $"{o.MachineName}.{o.JobName}.metadump.json"
      let skeleton = ChannelDto.FromJob(job)
      let json = JsonConvert.SerializeObject(skeleton, Formatting.Indented)
      cp $"Saving output to \fy{onm}\f0."
      do
        use w = onm |> startFile
        w.WriteLine(json)
      onm |> finishFile
      0

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
    | [] ->
      if o.JobName |> String.IsNullOrEmpty then
        cp "\foNo \fg-job\fo name specified\f0"
        None
      else
        o |> Some
    | x :: _ ->
      cp $"\foUnrecognized argument \fy{x}\f0."
      None
  let oo = args |> parsemore {
    JobName = null
    MachineName = Environment.MachineName
  }
  match oo with
  | Some(o) ->
    o |> runApp
  | None ->
    Usage.usage "metadump"
    1

