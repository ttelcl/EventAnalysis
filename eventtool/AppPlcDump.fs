module AppPlcDump

open System
open System.IO
open System.Diagnostics.Eventing.Reader

open Lcl.EventLog.Jobs
open Lcl.EventLog.Jobs.Database
open Lcl.EventLog.Utilities

open ColorPrint
open CommonTools
open Usage

type private Options = {
  RidFrom: int64 option
  RidTo: int64 option
  Channel: string
  Events: int list
  MachineName: string
  CompatibilityMode: bool
}

type private DumpFileEventRange = {
  FullName: string
  EventStart: int64
  EventEnd: int64
}

let run args =
  let rec parsemore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parsemore o
    | "-h" :: _ ->
      Usage.usage "plc-dump"
      exit 0
    | "-from" :: ridtxt :: rest
    | "-start" :: ridtxt :: rest
    | "-rid0" :: ridtxt :: rest ->
      let ok, rid = ridtxt |> Int64.TryParse
      if ok |> not then
        let key = args |> List.head
        failwith $"Expecting a number as argument to '{key}' but got '{ridtxt}'"
      rest |> parsemore {o with RidFrom = Some(rid)}
    | "-to" :: ridtxt :: rest
    | "-end" :: ridtxt :: rest
    | "-rid1" :: ridtxt :: rest ->
      let ok, rid = ridtxt |> Int64.TryParse
      if ok |> not then
        let key = args |> List.head
        failwith $"Expecting a number as argument to '{key}' but got '{ridtxt}'"
      rest |> parsemore {o with RidTo = Some(rid)}
    | "-job" :: channel :: rest
    | "-channel" :: channel :: rest ->
      rest |> parsemore {o with Channel = channel}
    | "-e" :: eidtxt :: rest ->
      let ok, eid = eidtxt |> Int32.TryParse
      if ok |> not then
        failwith $"Expecting a number as argument to '-e' but got '{eidtxt}'"
      rest |> parsemore {o with Events = eid :: o.Events}
    | "-m" :: machine :: rest ->
      rest |> parsemore {o with MachineName = machine}
    | [] ->
      let overridesJob = o.Channel |> String.IsNullOrEmpty |> not
      let hasEvents = o.Events |> List.isEmpty |> not
      let o =
        match overridesJob, hasEvents with
        | true, false ->
          failwith $"Specifying -job requires at least one -e as well"
        | false, true ->
          failwith $"Specifying -e requires -job as well"
        | false, false ->
          {o with Channel = "Security"; Events = [4688; 4689]; CompatibilityMode=true}
        | true, true ->
          {o with Events = o.Events |> List.rev; CompatibilityMode=false}
      o
    | x :: _ ->
      failwith $"Unrecognized argument '{x}'"
  let o = args |> parsemore {
    RidFrom = None
    RidTo = None
    Channel = null
    Events = []
    MachineName = Environment.MachineName
    CompatibilityMode = true
  }
  
  let edz = new EventDataZone(true, o.MachineName)
  if edz.Exists |> not then
    cp $"\foThe machine data zone \fb{o.MachineName}\fo does not exist (yet)\f0."
    failwith $"Unknown machine data zone '{o.MachineName}'"
  // Manually search the channel, since we want to prioritize channel over job here
  let cfg =
    let cfg = o.Channel |> edz.Registry.FindByChannel
    if cfg <> null then
      cfg
    else
      if o.Channel |> EventJobConfig.IsValidJobName then
        let cfg = o.Channel |> edz.Registry.FindByJob
        if cfg = null then
          failwith $"No channel or job named '{o.Channel}' found for machine zone '{o.MachineName}'"
        else
          cfg
      else
        failwith $"No channel named '{o.Channel}' found for machine zone '{o.MachineName}' (and it is not a valid job name)"
  let job = new EventJob(edz, cfg)
  let prefix, suffix =
    if o.CompatibilityMode then
      $"{o.MachineName}-events-", ".raw.xml"
    else
      $"{o.MachineName}-{job.Configuration.Name}.", ".xml"
  let mask = $"{prefix}*-*{suffix}"
  let maxrid = job.MaxRecordId1()
  let minrid = job.MinRecordId1()
  let breakDownFileName (fullName:string) =
    let fnm = fullName |> Path.GetFileName
    let tag = fnm.Substring(prefix.Length, fnm.Length-prefix.Length-suffix.Length)
    let parts = tag.Split('-')
    if parts.Length <> 2 then
      None
    else
      let ok1, id1 = parts[0] |> Int64.TryParse
      let ok2, id2 = parts[1] |> Int64.TryParse
      if ok1 && ok2 then
        Some({
          FullName = fullName
          EventStart = id1
          EventEnd = id2
        })
      else
        None
  if maxrid = 0L then
    cp "There are no records to export yet"
  else
    let rid0 =
      match o.RidFrom with
      | Some(rid) -> rid
      | None ->
        let ranges =
          Directory.EnumerateFiles(Environment.CurrentDirectory, mask)
          |> Seq.choose breakDownFileName
          |> Seq.toArray
        //for range in ranges do
        //  cp $"{range.EventStart} - {range.EventEnd} ({range.EventEnd - range.EventStart + 1L})"
        let lastId =
          if ranges.Length > 0 then
            ranges
            |> Seq.map (fun dfer -> dfer.EventEnd)
            |> Seq.max
          else
            -1L
        lastId + 1L
    let rid1 =
      match o.RidTo with
      | Some(rid) -> rid
      | None -> maxrid
    cp $"RID search range is {rid0} to {rid1} ({rid1 - rid0 + 1L})"
    if rid1 < rid0 then
      cp $"No events to export; The search range \fg{rid0}\f0 to \fg{rid1}\f0 is empty"
    else
      let mutable maxRid = rid0 - 1L
      let mutable minRid = rid1 + 1L
      let scratchName = $"{prefix}0-0{suffix}"
      do
        // fake XML: write it as text instead of using System.Xml
        use w = scratchName |> startFile
        w.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>")
        w.WriteLine("<EventContainer>")
        use odb = job.OpenInnerDatabase1(false)
        let records = odb.ReadEvents(ridMin=rid0, ridMax=rid1, reverse=true)
        for record in records do
          let eid = record.EventId
          let rid = record.RecordId
          if o.Events |> List.contains eid then
            if rid < minRid then
              minRid <- rid
            if rid > maxRid then
              maxRid <- rid
            record.Xml |> w.WriteLine
        w.WriteLine("</EventContainer>")
      if maxRid < minRid then
        cp $"No matching events found (in the selected RID range)"
      else
        let finalName = $"{prefix}{minRid}-{maxRid}{suffix}"
        scratchName |> finishFile2 finalName
        cp $"Saved \fg{finalName}\f0"
  0

