module AppJobs

open System
open System.Diagnostics.Eventing.Reader

open Lcl.EventLog.Jobs
open Lcl.EventLog.Jobs.Database
open Lcl.EventLog.Utilities

open ColorPrint
open CommonTools

type private Options = {
  Machine: string
  All: bool
}

let run args =
  let rec parsemore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parsemore o
    | "-h" :: _ ->
      Usage.usage "jobs"
      exit 0
    | "-M" :: rest ->
      if o.Machine |> String.IsNullOrEmpty |> not then
        failwith "-M and -m are mutually exclusive"
      rest |> parsemore {o with All = true}
    | "-m" :: mnm :: rest ->
      if o.All then
        failwith "-m and -M are mutually exclusive"
      rest |> parsemore {o with Machine = mnm}
    | [] ->
      o
    | x :: _ ->
      failwith $"Unrecognized argument '{x}'"
  let o = args |> parsemore {
    Machine = null
    All = false
  }
  let listZoneJobs (edz: EventDataZone) =
    cp $"Machine data zone \fb{edz.Machine}\f0:"
    let jobs = edz.EnumJobs() |> Seq.toList
    if jobs |> List.isEmpty then
      cp "\fo  No jobs yet\f0."
    else
      for job in jobs do
        let adminTag =
          if job.Admin then
            "\frAdmin\f0"
          else
            "\fk --- \f0"
        cp $"  \fc%-20s{job.Name} {adminTag} \fg{job.Channel}"
  if o.All then
    let anchor = new EventDataZone(true)
    let configurations = anchor.SiblingZones() |> Seq.toList
    if configurations |> List.isEmpty then
      cp "\foThere are no non-empty machine data zones yet\f0!"
    else
      for ezc in configurations do
        let edz = new EventDataZone(true, ezc.Machine)
        edz |> listZoneJobs
  else
    let machine =
      if o.Machine |> String.IsNullOrEmpty then
        Environment.MachineName
      else
        o.Machine
    let ezc = new EventZoneConfig(machine)
    let edz = new EventDataZone(true, ezc.Machine)
    edz |> listZoneJobs
  0

