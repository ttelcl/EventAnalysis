module AppJobSample

open System
open System.IO

open Lcl.EventLog.Jobs
open Lcl.EventLog.Jobs.Database
open Lcl.EventLog.Utilities

open ColorPrint
open CommonTools

type private JobSampleOptions = {
  Machine: string
  JobName: string
  EventCount: int
}

let private doRun o =
  let ezc = new EventZoneConfig(o.Machine)
  let edz = new EventDataZone(true, ezc.Machine)
  if edz.Exists |> not then
    failwith $"No data for machine '{ezc.Machine}'"
  let job = edz.TryOpenJob(o.JobName)
  if job = null then
    failwith $"No job or channel '{o.JobName}' known for machine '{o.Machine}'"
  if job.HasDbV2 |> not then
    failwith $"No data recorded yet for job '{o.JobName}'"
  let overviewRows = 
    job.GetOverview2(false) 
    |> Seq.toArray
    |> Array.sortBy (fun r -> (r.ProviderId, r.TaskId, r.EventId, r.EventVersion))
  let byProvider = overviewRows |> Array.groupBy (fun r -> r.ProviderId)
  let outputName = $"{job.Configuration.Name}.job-sample.xml"
  do
    let tmpName = outputName + ".tmp"
    cp $"Writing \fo{outputName}\f0."
    use xw = tmpName |> XmlUtilities.StartXmlFile
    use odb = job.OpenInnerDatabase2(false)
    xw.WriteStartElement("job-sample")
    for (prvId,prvRows) in byProvider do
      let prvName = prvRows[0].ProviderName
      let prvName = if prvName |> String.IsNullOrEmpty then "?" else prvName
      xw.WriteStartElement("provider")
      xw.WriteAttributeString("id", $"{prvId}")
      xw.WriteAttributeString("name", prvName)
      let byTask = prvRows |> Array.groupBy (fun r -> r.TaskId)
      for (tid, taskRows) in byTask do
        xw.WriteStartElement("task")
        xw.WriteAttributeString("id", $"{tid}")
        let taskDesc = taskRows[0].TaskDescription
        if taskDesc |> String.IsNullOrEmpty |> not then
          xw.WriteAttributeString("description", taskDesc)
        for taskRow in taskRows do
          xw.WriteStartElement("event-sample")
          xw.WriteAttributeString("event", $"{taskRow.EventId}")
          xw.WriteAttributeString("version", $"{taskRow.EventVersion}")
          xw.WriteAttributeString("operation-id", $"{taskRow.OperationId}")
          let opdesc = taskRow.OperationDescription
          if opdesc |> String.IsNullOrEmpty |> not then
            xw.WriteAttributeString("operation", opdesc)
          if o.EventCount > 0 then
            let events =
              odb.QueryEvents(
                eid = taskRow.EventId,
                prvid = taskRow.ProviderId,
                reverse = true,
                limit = o.EventCount,
                task = taskRow.TaskId,
                ever = taskRow.EventVersion,
                opid = taskRow.OperationId)
            for evt in events do
              let xml = evt.Xml
              xml |> xw.AppendXml
          xw.WriteEndElement()
        xw.WriteEndElement()
      xw.WriteEndElement()
    xw.WriteEndElement()
  outputName |> finishFile
  0

let run args =
  let rec parseMore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parseMore o
    | "-h" :: _ ->
      None
    | "-n" :: ntxt :: rest ->
      let n = ntxt |> Int32.Parse
      if n < 0 then
        failwith "Event count (-n) should be >= 0"
      rest |> parseMore {o with EventCount = n}
    | "-job" :: jnm :: rest ->
      rest |> parseMore {o with JobName = jnm}
    | "-m" :: mnm :: rest ->
      rest |> parseMore {o with Machine = mnm}
    | [] ->
      if o.JobName |> String.IsNullOrEmpty then
        failwith "No Job name (-job) provided."
      Some(o)
    | x :: _ ->
      failwith $"Unrecognized argument '{x}'"
  let oo = args |> parseMore {
    Machine = Environment.MachineName
    JobName = null
    EventCount = 1
  }
  match oo with
  | Some(o) ->
    o |> doRun
  | None ->
    Usage.usage "job-sample"
    0
