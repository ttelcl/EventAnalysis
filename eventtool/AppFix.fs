module AppFix

open System

open Lcl.EventLog.Jobs
open Lcl.EventLog.Jobs.Database
open Lcl.EventLog.Utilities

open ColorPrint
open CommonTools

type private Options = {
  JobName: string
  Machine: string
}

let run args =
  let rec parsemore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parsemore o
    | "-h" :: _ ->
      Usage.usage "fix"
      exit 0
    | "-job" :: jnm :: rest ->
      rest |> parsemore {o with JobName = jnm}
    | "-machine" :: mnm :: rest ->
      rest |> parsemore {o with Machine = mnm}
    | [] ->
      if o.JobName |> String.IsNullOrEmpty then
        failwith $"Missing job name (-job)"
      o
    | x :: _ ->
      failwith $"Unrecognized argument '{x}'"
  let o = args |> parsemore {
    JobName = null
    Machine = Environment.MachineName
  }  
  let zone = new EventDataZone(false, o.Machine)
  let job = o.JobName |> zone.TryOpenJob
  if job = null then
    failwith $"Unknown job or channel name '{o.JobName}' for machine '{o.Machine}'"
  if job.HasDbV1 then
    cp $"V1 DB already exists: \fg{job.RawDbFileV1}\f0"
  else
    cp $"V1 DB is missing: \fo{job.RawDbFileV1}\f0"
  if job.HasDbV2 then
    cp $"V2 DB already exists: \fg{job.RawDbFileV2}\f0"
  else
    cp $"V2 DB is missing: \fo{job.RawDbFileV2}\f0"
  if job.HasDbV1 && job.HasDbV2 then
    cp "\fgBoth databases already exist\f0."
  else
    cp "\fyInitializing missing databases\f0"
    job.InitDb()
  0

