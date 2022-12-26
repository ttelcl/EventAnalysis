module AppSamples

open System
open System.Diagnostics.Eventing.Reader

open Lcl.EventLog.Jobs
open Lcl.EventLog.Jobs.Database
open Lcl.EventLog.Utilities

open ColorPrint
open CommonTools
open System.IO

type private Options = {
  Machine: string
  JobName: string
  ProviderKey: string
  EventId: int
  EventCount: int
}

let run args =
  let rec parsemore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parsemore o
    | "-h" :: _ ->
      Usage.usage "samples"
      exit 0
    | "-n" :: ntxt :: rest ->
      let n = ntxt |> Int32.Parse
      if n <= 0 then
        failwith "Event count (-n) should be > 0"
      rest |> parsemore {o with EventCount = n}
    | "-e" :: etxt :: rest ->
      let e = etxt |> Int32.Parse
      if e < 0 then
        failwith "Event ID (-e) should be >= 0"
      rest |> parsemore {o with EventId = e}
    | "-p" :: prvk :: rest ->
      rest |> parsemore {o with ProviderKey = prvk}
    | "-job" :: jnm :: rest ->
      rest |> parsemore {o with JobName = jnm}
    | "-m" :: mnm :: rest ->
      rest |> parsemore {o with Machine = mnm}
    | [] ->
      if o.EventId < 0 then
        cp "\frNo event ID (\fg-e\fr) provided\f0. Use the '\foeventtool overview\f0' command to find known IDs for a job."
        failwith "No event ID (-e) provided."
      if o.JobName |> String.IsNullOrEmpty then
        failwith "No Job name (-job) provided."
      o
    | x :: _ ->
      failwith $"Unrecognized argument '{x}'"
  let o = args |> parsemore {
    Machine = Environment.MachineName
    JobName = null
    ProviderKey = null
    EventId = -1
    EventCount = 2
  }
  let ezc = new EventZoneConfig(o.Machine)
  let edz = new EventDataZone(true, ezc.Machine)
  if edz.Exists |> not then
    failwith $"No data for machine '{ezc.Machine}'"
  let job = edz.TryOpenJob(o.JobName)
  if job = null then
    failwith $"No job or channel '{o.JobName}' known for machine '{o.Machine}'"
  if job.HasDbV1 |> not then
    failwith $"No data recorded yet for job '{o.JobName}'"
  let provider =
    let dors =
      job.GetOverview2(false)
      |> Seq.where (fun dor -> dor.EventId = o.EventId)
      |> Seq.toArray
    match dors.Length with
    | 0 ->
      failwith $"No (meta)data for event {o.EventId}"
    | 1 ->
      if o.ProviderKey |> String.IsNullOrEmpty |> not then
        cp $"There is only one provider for event \fc{o.EventId}\f0. Ignoring \fg-p\f0 option."
      dors[0]
    | _ ->
      if o.ProviderKey |> String.IsNullOrEmpty then
        cp $"There are multiple providers for event \fc{o.EventId}\f0."
        cp "Please use a \fg-p\f0 argument to disambiguate:"
        for dor in dors do
          cp $"  \fb%2d{dor.ProviderId}\f0 = \fy{dor.ProviderName}\f0."
        failwith "Ambiguous event id. A provider is required to disambiguate"
      let ok, prvid = o.ProviderKey |> Int32.TryParse
      if ok then
        let dors = dors |> Array.where (fun dor -> dor.ProviderId = prvid)
        match dors.Length with
        | 0 -> failwith $"Disambiguation failed. No providers for event {o.EventId} with provider ID {prvid} found"
        | 1 -> dors[0]
        | n ->
          failwith $"Internal Error! Disambiguation failed. {n} providers for event {o.EventId} with provider ID {prvid} found"
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
            failwith $"Disambiguation failed. No providers for event {o.EventId} matching provider key '{o.ProviderKey}' found"
          | 1 -> dors[0]
          | n ->
            failwith $"Disambiguation failed. {n} distinct providers for event {o.EventId} matching key '{o.ProviderKey}' found"
        | 1 -> dors[0]
        | n -> 
          failwith $"Internal Error! Disambiguation failed. {n} providers for event {o.EventId} with provider name {o.ProviderKey} found"
  cp $"Looking for events with ID \fg{o.EventId}\f0 in provider '\fy{provider.ProviderName}\f0' (provider id \fb{provider.ProviderId}\f0)"
  let recordIds =
    use odb = job.OpenInnerDatabase2(false)
    odb.QueryEventIds(eid = o.EventId, prvid = provider.ProviderId)
    |> Seq.toArray
  cp $"Found \fg{recordIds.Length}\f0 records matching \fc-e {o.EventId}\f0"
  if recordIds.Length = 0 then
    cp $"\frNo events available\f0!"
  else
    let ecount =
      if recordIds.Length < o.EventCount then
        cp $"\foNot enough events available to meet the request\f0. Using \fg-n \fc{recordIds.Length}\f0 instead"
        recordIds.Length
      else
        o.EventCount
    cp $"Picking \fb{ecount}\f0 events from \fc{recordIds.Length}\f0 available events"
    let selection =
      if ecount > 1 then
        let picker i =
          let idx = ((recordIds.Length-1) * i) / (ecount-1)
          recordIds[idx]
        Array.init (ecount) picker
      else
        // special case for n = 1: pick most recent record
        [| recordIds[recordIds.Length - 1] |]
    use odb = job.OpenInnerDatabase2(false)
    for rid in selection do
      let e = odb.FindEvent(rid)
      if e = null then
        cp $"\frRecord \fy{rid}\fr not found\f0!"
      else
        let onm = $"{job.Zone.Machine}.{job.Configuration.Name}.P%02d{e.ProviderId}.E%05d{e.EventId}.R%07d{rid}.xml"
        cp $"Saving \fg{onm}\f0"
        let xml =
          XmlUtilities.IndentXml(e.Xml, false)
        File.WriteAllText(onm, xml)
  0



