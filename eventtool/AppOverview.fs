module AppOverview

open System
open System.Diagnostics.Eventing.Reader

open Lcl.EventLog.Jobs
open Lcl.EventLog.Jobs.Database
open Lcl.EventLog.Utilities

open ColorPrint
open CommonTools

type private Options = {
  JobName: string
  MachineName: string
  WithSize: bool
  Save: bool // NYI!
}

let run args =
  let rec parsemore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parsemore o
    | "-h" :: _ ->
      Usage.usage "overview"
      exit 0
    | "-save" :: rest ->
      rest |> parsemore {o with Save = true}
    | "-job" :: jnm :: rest
    | "-channel" :: jnm :: rest ->
      rest |> parsemore {o with JobName = jnm}
    | "-machine" :: mnm :: rest
    | "-m" :: mnm :: rest ->
      rest |> parsemore {o with MachineName = mnm}
    | "-nosize" :: rest ->
      rest |> parsemore {o with WithSize = false}
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
    Save = false
    WithSize = true
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
  cp $"Inspecting database \fy{job.RawDbFileV1}\f0."
  let overview = job.GetOverview1(o.WithSize)
  if overview.Count = 0 then
    cp $"\foThe database appears to be empty\f0!"
    1
  else
    let nonempty =
      overview
      |> Seq.where (fun o -> o.EventCount > 0)
      |> Seq.toArray
    if nonempty.Length = 0 then
      cp $"\foThere are no events recorded in the database\f0, but some metadata is present."
      for r in overview do
        let enabled =
          if r.IsEnabled then "\fgEnabled\f0, " else "\frDisabled\f0,"
        let taskLabel =
          if r.TaskLabel |> String.IsNullOrEmpty then
            "\fk(no description)\f0"
          else
            $"'{r.TaskLabel}'"
        cpx $"(\fg%5d{r.EventId}\f0,\fc%5d{r.TaskId}\f0) {enabled} {taskLabel}"
      0
    else
      let ridMin = nonempty |> Seq.map (fun r -> r.MinRid) |> Seq.min
      let ridMax = nonempty |> Seq.map (fun r -> r.MaxRid) |> Seq.max
      let tMin = nonempty |> Seq.map (fun r -> r.UtcMin.Value) |> Seq.min
      let tMax = nonempty |> Seq.map (fun r -> r.UtcMax.Value) |> Seq.max
      let tMinTxt = tMin.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff")
      let tMaxTxt = tMax.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fffffff")
      let count = nonempty |> Seq.sumBy (fun r -> r.EventCount)
      
      cp $"There are \fb{count}\f0 events in the database:"
      cp $"  Event Record ID range is \fy{ridMin}\f0 to \fy{ridMax}\f0."
      cp $"  Time range (local) is \fg{tMinTxt}\f0 to \fg{tMaxTxt}\f0"
      if o.WithSize then
        let size = nonempty |> Seq.sumBy (fun r -> r.XmlSize)
        cp $"  Total XML characters: \fb{size}\f0 (average \fC{size / int64(count)}\f0)"

      cp "Events and tasks info"
      cpx "\fxEvent  \f0|\fx Task  \f0|\fx vmin \f0|\fx State    \f0|\fx Count   \f0|"
      if o.WithSize then
        cpx "\fx   Size   ( avg ) \f0|"
      cp "\fx RID from - to     \f0|\fx task label \f0"
      for r in overview do
        let enabled =
          if r.IsEnabled then "\fgEnabled " else "\frDisabled"
        let taskLabel =
          if r.TaskLabel |> String.IsNullOrEmpty then
            "\fk(no description)"
          else
            $"{r.TaskLabel}"
        let avgSize =
          if r.EventCount = 0 then
            "    -"
          else
            $"%5d{r.XmlSize/int64(r.EventCount)}"
        cpx $"\fg%6d{r.EventId} \f0|\fc%6d{r.TaskId} \f0|\fk%5d{r.MinVersion} \f0| {enabled} \f0|"
        cpx $"\fb%8d{r.EventCount} \f0|"
        if o.WithSize then
          cpx $"\fw%9d{r.XmlSize}\f0 (\fC{avgSize}\f0) |"
        cp $"\fy%8d{r.MinRid}\f0 -\fy%8d{r.MaxRid} \f0| {taskLabel}\f0"
      
      if o.Save then
        cp $"\fg-save\fo is not implemented yet\f0."
        1
      else
        0

