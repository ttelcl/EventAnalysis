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
  UpdateV1: bool
  UpdateV2: bool
}

let run args =
  let rec parsemore o args =
    // Any -v and -tag options already have been processed and removed
    match args with
    | "-db1" :: rest ->
      rest |> parsemore {o with UpdateV1 = true}
    | "-db2" :: rest ->
      rest |> parsemore {o with UpdateV2 = true}
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
    UpdateV1 = false
    UpdateV2 = false
  }
  let zone = new EventDataZone(false)
  let jobs = o.JobNames |> List.map zone.OpenJob
  let v1, v2 =
    match o.UpdateV1, o.UpdateV2 with
    | false, false ->
      cp "No -db1 nor -db2 specified. Defaulting to -db1"
      true, false
    | v1, v2 ->
      v1, v2
  for job in jobs do
    if v1 then
      cpx $"Job \fg{job.Configuration.Name}\f0 (\fc{job.Configuration.Channel}\f0) \foV1 DB\f0: "
      let n = job.UpdateDb1(o.Cap)
      let mrid = job.MaxRecordId1()
      if n < o.Cap then
        cp $"\fg{n}\f0 / \fb{o.Cap}\f0 records (max=\fy{mrid}\f0)."
      else
        cp $"\fr{n}\f0 / \fb{o.Cap}\f0 records (max=\fy{mrid}\f0). \foMore available\f0."
    if v2 then
      cpx $"Job \fg{job.Configuration.Name}\f0 (\fc{job.Configuration.Channel}\f0) \foV2 DB\f0: "
      let n = job.UpdateDb2(o.Cap)
      let mrid = job.MaxRecordId2()
      if n < o.Cap then
        cp $"\fg{n}\f0 / \fb{o.Cap}\f0 records (max=\fy{mrid}\f0)."
      else
        cp $"\fr{n}\f0 / \fb{o.Cap}\f0 records (max=\fy{mrid}\f0). \foMore available\f0."
  0

