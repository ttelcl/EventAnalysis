module AppJobs

open System
open System.IO

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
    cp $"Machine data zone \fb{edz.Machine}\f0 (\fG{edz.RootFolder}\f0):"
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
        let hasV1, hasV2, fnm2 =
          let ejob = edz.TryOpenJob(job.Name)
          if ejob = null then
            false, false, null
          else
            ejob.HasDbV1, ejob.HasDbV2, ejob.RawDbFileV2
        let v1tag = if hasV1 then "\fGv1.\f0" else "\fRV1!\f0"
        let v2tag = if hasV2 then "\fgV2.\f0" else "\frV2!\f0"
        let v2date, v2size =
          if hasV2 then
            let fi2 = new FileInfo(fnm2)
            fi2.LastWriteTime.ToString("yyyy-MM-dd"), fi2.Length.ToString()
          else
            "          ", ""
        cp $"  \fc%-20s{job.Name} {adminTag} {v1tag} {v2tag} \f0{v2date} \fb{v2size,11} \fy{job.Channel}"
  if o.All then
    let anchor = new EventDataZone(true)
    cp $"Data folder is \fo{anchor.BaseFolder}\f0"
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
    cp $"Data folder is \fo{edz.BaseFolder}\f0"
    edz |> listZoneJobs
  0

