module AppOverview2

open System
open System.Diagnostics.Eventing.Reader

open XsvLib
open XsvLib.Buffers

open Lcl.EventLog.Jobs
open Lcl.EventLog.Jobs.Database
open Lcl.EventLog.Utilities

open ColorPrint
open CommonTools

type private Options = {
  JobName: string
  MachineName: string
  EventCounts: bool
}

let private missingString def s = if s |> String.IsNullOrEmpty then def else s

let run args =
  let rec parsemore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parsemore o
    | "-h" :: _ ->
      Usage.usage "overview"
      exit 0
    | "-machine" :: mnm :: rest
    | "-m" :: mnm :: rest ->
      rest |> parsemore {o with MachineName = mnm}
    | "-job" :: jnm :: rest
    | "-channel" :: jnm :: rest ->
      rest |> parsemore {o with JobName = jnm}
    | "-counts" :: rest ->
      rest |> parsemore {o with EventCounts = true}
    | [] ->
      if o.JobName |> String.IsNullOrEmpty then
        cp "\frNo job or channel (\fg-job\fr) provided\f0. Use \foeventtool jobs\f0 to find known job names."
        failwith "No job (or channel) specified"
      o
    | x :: _ ->
      failwith $"unrecognized option '{x}'"
  let o = args |> parsemore {
    JobName = null
    MachineName = Environment.MachineName
    EventCounts = false
  }
  let edz = new EventDataZone(true, o.MachineName)
  if edz.Exists |> not then
    cp $"\foThe machine data zone \fb{o.MachineName}\fo does not exist (yet)\f0."
    failwith $"Unknown machine data zone '{o.MachineName}'"
  let job = edz.TryOpenJob(o.JobName)
  if job = null then
    cp $"\foNo job or channel named \fc{o.JobName}\fo found in machine data zone \fb{o.MachineName}\f0."
    failwith $"Unknown job or channel '{o.JobName}' in machine data zone '{o.MachineName}'"
  cp $"Job \fc{job.Configuration.Name}\f0 is channel \fg{job.Configuration.Channel}\f0 in machine zone \fb{edz.Machine}\f0."
  cp $"Inspecting database \fy{job.RawDbFileV2}\f0."
  let overviews = job.GetOverview2(o.EventCounts)
  if overviews.Count > 0 then
    if o.EventCounts then
      let totalEvents = overviews |> Seq.sumBy (fun ovr -> ovr.EventCount)
      cp $"Saving \fb{overviews.Count}\f0 records (\fg{totalEvents}\f0 events)"
    else
      cp $"Saving \fb{overviews.Count}\f0 records."
    do
      let buff = new XsvBuffer(true)
      let colIndex = buff.Declare<int>("id")
      let colPrvName = buff.Declare<string>("provider")
      let colPrvId = buff.Declare<int>("prvid")
      let colEventId = buff.Declare<int>("event")
      let colVersion = buff.Declare<int>("e_ver")
      let colTaskId = buff.Declare<int>("task")
      let colOpcode = buff.Declare<int>("opcode")
      let colTaskDesc = buff.Declare<string>("taskdesc")
      let colOpDesc = buff.Declare<string>("opdesc")
      let colOptEvtCount =
        if o.EventCounts then
          buff.Declare<int>("eventcount") |> Some
        else
          None
      buff.Lock()

      let onm = $"{job.Configuration.Name}.overview.csv"
      do
        use w = onm |> startFile
        let itrw = Xsv.WriteXsv(w, onm, buff.Count)
        buff.WriteHeader(itrw)
        for index, overview in overviews |> Seq.indexed do
          index |> colIndex.Set
          overview.ProviderName |> missingString "?" |> colPrvName.Set
          overview.ProviderId |> colPrvId.Set
          overview.EventId |> colEventId.Set
          overview.EventVersion |> colVersion.Set
          overview.TaskId |> colTaskId.Set
          overview.OperationId |> colOpcode.Set
          overview.TaskDescription |> missingString "?" |> colTaskDesc.Set
          overview.OperationDescription |> missingString "?" |> colOpDesc.Set
          match colOptEvtCount with
          | Some(colEvtCount) ->
            overview.EventCount |> colEvtCount.Set
          | None ->
            ()
          buff.WriteRow(itrw)
      onm |> finishFile
    do
      let prvEventCount =
        overviews
        |> Seq.groupBy (fun ovr -> ovr.ProviderName, ovr.ProviderId, ovr.EventId)
        |> Seq.map (fun ((pn,pi,eid),items) -> (pn, pi, eid, items |> Seq.toArray |> Array.length, items |> Seq.sumBy (fun dor -> dor.EventCount)))
        |> Seq.toArray
      let buff = new XsvBuffer(true)
      let colPrvName = buff.Declare<string>("provider")
      let colPrvId = buff.Declare<int>("prvid")
      let colEventId = buff.Declare<int>("event")
      let colCount = buff.Declare<int>("taskops")
      let colOptEvCount = if o.EventCounts then Some(buff.Declare<int>("evcount")) else None
      buff.Lock()
      let onm = $"{job.Configuration.Name}.provider-event-counts.csv"
      do
        use w = onm |> startFile
        let itrw = Xsv.WriteXsv(w, onm, buff.Count)
        buff.WriteHeader(itrw)
        for (pn, pi, eid, n, nev) in prvEventCount do
          if n > 1 then
            cp $"  \foWarning\f0: (\fy{pn}\f0,\fg{eid}\f0) does not uniquely identify a (task,opcode)! (\fb{n}\f0)"
          pn |> missingString "?" |> colPrvName.Set
          pi |> colPrvId.Set
          eid |> colEventId.Set
          n |> colCount.Set
          match colOptEvCount with
          | Some(colEvCount) -> nev |> colEvCount.Set
          | None -> ()
          buff.WriteRow(itrw)
      onm |> finishFile
    0
  else
    cp $"\foThis database is empty!\f0 (no files saved)"
    1



