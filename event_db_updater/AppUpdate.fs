module AppUpdate


open System
open System.Diagnostics.Eventing.Reader

open Lcl.EventLog.Jobs
open Lcl.EventLog.Jobs.Database
open Lcl.EventLog.Utilities

open ColorPrint
open CommonTools

type private Options = {
  JobNames: string list
  Cap: int
}

let run args =
  let rec parsemore o args =
    // Any -v and -tag options already have been processed and removed
    match args with
    | "-job" :: jnm :: rest ->
      rest |> parsemore {o with JobNames = jnm :: o.JobNames}
    | "-cap" :: captxt :: rest ->
      let cap = captxt |> Int32.Parse
      rest |> parsemore {o with Cap = cap}
    | [] ->
      if o.JobNames |> List.isEmpty then
        failwith "Missing -job argument: expecting at least 1 job or channel name"
      if o.Cap <= 0 then
        failwith "Invalid -cap argument"
      {o with JobNames = o.JobNames |> List.rev}
    | x :: _ ->
      failwith $"Unknown command '{x}'"
  let o = args |> parsemore {
    JobNames = []
    Cap = 20000
  }
  let zone = new EventDataZone(false)
  let jobs = o.JobNames |> List.map zone.OpenJob
  for job in jobs do
    cpx $"Job \fg{job.Configuration.Name}\f0 (\fc{job.Configuration.Channel}\f0): "
    let n = job.UpdateDb1(o.Cap)
    let mrid = job.MaxRecordId1()
    if n < o.Cap then
      cp $"\fg{n}\f0 / \fb{o.Cap}\f0 records (max=\fy{mrid}\f0)."
    else
      cp $"\fr{n}\f0 / \fb{o.Cap}\f0 records (max=\fy{mrid}\f0). \foMore available\f0."
  0

