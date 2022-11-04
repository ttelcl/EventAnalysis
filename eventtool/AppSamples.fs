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
      if e <= 0 then
        failwith "Event ID (-e) should be > 0"
      rest |> parsemore {o with EventId = e}
    | "-job" :: jnm :: rest ->
      rest |> parsemore {o with JobName = jnm}
    | "-m" :: mnm :: rest ->
      rest |> parsemore {o with Machine = mnm}
    | [] ->
      if o.EventId <= 0 then
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
    EventId = 0
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
  let recordIds =
    use odb = job.OpenInnerDatabase(false)
    odb.ReadEventIdsTicks(eid = o.EventId)
    |> Seq.toArray
  cp $"Found \fg{recordIds.Length}\f0 records matching \fc-e {o.EventId}\f0"
  if recordIds.Length = 0 then
    cp $"\frNo events available\f0!"
  elif recordIds.Length < o.EventCount then
    cp $"\foNot enough events available to meet the request\f0 (\fb{o.EventCount}\f0)"
  else
    let picker i =
      let idx = ((recordIds.Length-1) * i) / (o.EventCount-1)
      recordIds[idx]
    let selection = Array.init (o.EventCount) picker
    use odb = job.OpenInnerDatabase(false)
    for rid in selection do
      let e = odb.ReadEvent(rid)
      if e = null then
        cp $"\frRecord \fy{rid}\fr not found\f0!"
      else
        let onm = $"{job.Zone.Machine}.{job.Configuration.Name}.E%05d{e.EventId}.R%07d{rid}.xml"
        cp $"Saving \fg{onm}\f0"
        let xml =
          XmlUtilities.IndentXml(e.Xml, false)
        File.WriteAllText(onm, xml)
  0



