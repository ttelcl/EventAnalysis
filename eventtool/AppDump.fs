module AppDump

open System

open XsvLib
open XsvLib.Buffers

open Lcl.EventLog.Jobs
open Lcl.EventLog.Jobs.Database
open Lcl.EventLog.Utilities
open Lcl.EventLog.Utilities.Xml

open ColorPrint
open CommonTools

type private DumpMode =
  | Normal
  | List

type private Options = {
  RidTo: int64 option
  MaxCount: int
  JobName: string
  ProviderKey: string
  EventId: int option
  MachineName: string
  Mode: DumpMode
}

let private getProvider o (job:EventJob) =
  let eventId =
    match o.EventId with
    | Some(eventId) -> eventId
    | None ->
      failwith "No event ID specified"
  let dors =
    job.GetOverview2(false)
    |> Seq.where (fun dor -> dor.EventId = eventId)
    |> Seq.toArray
  match dors.Length with
  | 0 ->
    failwith $"No (meta)data for event {eventId}"
  | 1 ->
    if o.ProviderKey |> String.IsNullOrEmpty |> not then
      cp $"There is only one provider for event \fc{eventId}\f0. Ignoring \fg-p\f0 option."
    dors[0]
  | _ ->
    if o.ProviderKey |> String.IsNullOrEmpty then
      cp $"There are multiple providers for event \fc{eventId}\f0."
      cp "Please use a \fg-p\f0 argument to disambiguate:"
      for dor in dors do
        cp $"  \fb%2d{dor.ProviderId}\f0 = \fy{dor.ProviderName}\f0."
      failwith "Ambiguous event id. A provider is required to disambiguate"
    let ok, prvid = o.ProviderKey |> Int32.TryParse
    if ok then
      let dors = dors |> Array.where (fun dor -> dor.ProviderId = prvid)
      match dors.Length with
      | 0 -> failwith $"Disambiguation failed. No providers for event {eventId} with provider ID {prvid} found"
      | 1 -> dors[0]
      | n ->
        failwith $"Internal Error! Disambiguation failed. {n} providers for event {eventId} with provider ID {prvid} found"
    else
      // First try exact match
      let dors0 = dors
      let dors = dors0 |> Array.where (fun dor -> StringComparer.OrdinalIgnoreCase.Equals(o.ProviderKey, dor.ProviderName))
      match dors.Length with
      | 0 ->
        // Try partial match instead
        let dors = dors0 |> Array.where (fun dor -> dor.ProviderName.Contains(o.ProviderKey, StringComparison.OrdinalIgnoreCase))
        match dors.Length with
        | 0 ->
          failwith $"Disambiguation failed. No providers for event {eventId} matching provider key '{o.ProviderKey}' found"
        | 1 -> dors[0]
        | n ->
          failwith $"Disambiguation failed. {n} distinct providers for event {eventId} matching key '{o.ProviderKey}' found"
      | 1 -> dors[0]
      | n -> 
        failwith $"Internal Error! Disambiguation failed. {n} providers for event {eventId} with provider name {o.ProviderKey} found"

let run args =
  let rec parsemore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parsemore o
    | "-h" :: _ ->
      Usage.usage "dump"
      exit 0
    | "-e" :: eventText :: rest ->
      let ok, event = eventText |> Int32.TryParse
      if ok |> not then
        failwith $"Expecting an integer as event id, but found '{eventText}'"
      rest |> parsemore {o with EventId = Some(event)}
    | "-job" :: jobname :: rest ->
      rest |> parsemore {o with JobName = jobname}
    | "-p" :: provider :: rest ->
      rest |> parsemore {o with ProviderKey = provider}
    | "-n" :: countText :: rest ->
      let ok, count = countText |> Int32.TryParse
      if ok |> not then
        failwith $"Expecting an integer as maximum count, but found '{countText}'"
      rest |> parsemore {o with MaxCount = count}
    | "-N" :: rest ->
      rest |> parsemore {o with MaxCount = Int32.MaxValue}
    | "-to" :: ridText :: rest
    | "-rid" :: ridText :: rest ->
      let ok, ridTo = ridText |> Int32.TryParse
      if ok |> not then
        failwith $"Expecting an integer as RID cap, but got '{ridText}'"
      rest |> parsemore {o with RidTo = Some(ridTo)}
    | "-machine" :: mnm :: rest
    | "-m" :: mnm :: rest ->
      rest |> parsemore {o with MachineName = mnm}
    | "-list" :: rest ->
      rest |> parsemore {o with Mode = DumpMode.List}
    | [] ->
      if o.EventId |> Option.isNone then
        failwith "Missing event ID"
      o
    | x :: _ ->
      failwith $"Unrecognized argument '%s{x}'"
  let o = args |> parsemore {
    RidTo = None
    MaxCount = 1000
    JobName = null
    ProviderKey = null
    EventId = None
    MachineName = Environment.MachineName
    Mode = DumpMode.Normal
  }
  if o.JobName |> String.IsNullOrEmpty then
    cp "\frMissing \fo-job\fr option: No job (channel) name given\f0."
    1
  else if o.EventId |> Option.isNone then
    cp "\frMissing \fo-e\fr option: No event ID given\f0."
    1
  else
    let ezc = new EventZoneConfig(o.MachineName)
    let edz = new EventDataZone(true, ezc.Machine)
    if edz.Exists |> not then
      failwith $"No data for machine '{ezc.Machine}'"
    let job = edz.TryOpenJob(o.JobName)
    if job = null then
      failwith $"No job or channel '{o.JobName}' known for machine '{o.MachineName}'"
    if job.HasDbV2 |> not then
      failwith $"No data recorded yet for job '{o.JobName}'"
    let provider = getProvider o job
    cp $"Looking for events with ID \fg{o.EventId.Value}\f0 in provider '\fy{provider.ProviderName}\f0' (provider id \fb{provider.ProviderId}\f0)"
    use odb = job.OpenInnerDatabase2(false)
    let event0 = 
      let event00 =
        odb.QueryEventHeaders(
          ridMax = (o.RidTo |> Option.toNullable),
          eid = Nullable(o.EventId.Value),
          prvid = Nullable(provider.ProviderId),
          reverse = true,
          limit = 1)
        |> Seq.tryHead
      match event00 with
      | Some(event) -> event
      | None ->
        failwith $"No matching events found"
    let exr = odb.FindEventXml(event0)
    if exr = null then
      failwith $"DB error: XML blob missing for header RID {event0.RecordId}"
    let dissector = new XmlEventDissector(exr.Xml)
    let fieldmap = dissector.MapData()

    match o.Mode with
    | DumpMode.List ->
      for kvp in fieldmap do
        cp $"  \fc%24s{kvp.Key} \f0= \fg{kvp.Value}\f0"
      0
    | DumpMode.Normal ->
      let onm = $"dump.{o.JobName}.P{event0.ProviderId}.E%05d{event0.EventId}.csv"
      let buff = new XsvBuffer(true)
      let colRid = buff.Declare<int64>("rid")
      let colStamp = buff.Declare<int64>("eventstamp")
      let colTime = buff.Declare<string>("eventtime")
      let colEvent = buff.Declare<int>("event")
      let dataColumns =
        fieldmap.Keys
        |> Seq.map (fun key -> (key, buff.Declare<string>(key)))
        |> Map.ofSeq
      buff.Lock()
      do
        use w = onm |> startFile
        let itrw = Xsv.WriteXsv(w, onm, buff.Count)
        buff.WriteHeader(itrw)
        cp "\fyLoading events...\f0."
        let events =
          odb.QueryEventHeaders(
            ridMax = (o.RidTo |> Option.toNullable),
            eid = Nullable(o.EventId.Value),
            prvid = Nullable(provider.ProviderId),
            reverse = true,
            limit = o.MaxCount)
          // note: dapper defaults to buffered mode. No need for ".toArray()"
        cp "\fySaving events...\f0."
        let mutable rowCount = 0
        for event in events do
          event.RecordId |> colRid.Set
          let stamp = event.Stamp
          stamp |> colStamp.Set
          let dto = stamp.EpochDateTimeOffset()
          let t = dto.ToString("yyyy-MM-dd HH:mm:ss.fffffff")
          t |> colTime.Set
          event.EventId |> colEvent.Set
          let exr = odb.FindEventXml(event)
          if exr = null then
            failwith $"DB error: XML blob missing for header RID {event.RecordId}"
          let dissector = new XmlEventDissector(exr.Xml)
          for kvp in dataColumns do
            let dataValue = dissector.EvalData(kvp.Key)
            if String.IsNullOrEmpty(dataValue) then
              String.Empty |> kvp.Value.Set
            else
              dataValue |> kvp.Value.Set
          buff.WriteRow(itrw)
          rowCount <- rowCount + 1
        cp $"\fgSaved \fb{rowCount}\fg rows\f0."
      onm |> finishFile
      0

