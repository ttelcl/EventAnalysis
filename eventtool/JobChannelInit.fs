module JobChannelInit

open System
open System.Diagnostics.Eventing.Reader

open Lcl.EventLog.Jobs
open Lcl.EventLog.Jobs.Database
open Lcl.EventLog.Utilities

open ColorPrint
open CommonTools

type private Options = {
  JobName: string
  AutoJobName: bool
  ChannelName: string
  Machine: string
  IsLocal: bool
  Admin: bool
}

let private isblank s =
  s |> String.IsNullOrEmpty

let deriveJobName (channel:string) =
  let parts0 = channel.Split("/")
  let parts1 = parts0[0].Split("-")
  let parts2 = parts1[parts1.Length-1].Split(" ")
  String.Join("-", parts2).ToLowerInvariant()

let run args =
  let rec parsemore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parsemore o
    | "-h" :: _ ->
      Usage.usage "init"
      exit 0
    | "-job" :: jnm :: rest ->
      if o.AutoJobName then
        failwith "-job and -J are mutually exclusive"
      rest |> parsemore {o with JobName = jnm}
    | "-J" :: rest ->
      if o.JobName |> String.IsNullOrEmpty |> not then
        failwith "-J and -job are mutually exclusive"
      rest |> parsemore {o with AutoJobName = true}
    | "-channel" :: chnm :: rest ->
      rest |> parsemore {o with ChannelName = chnm}
    | "-machine" :: mnm :: rest ->
      rest |> parsemore {o with Machine = mnm}
    | "-admin" :: rest ->
      rest |> parsemore {o with Admin = true}
    | [] ->
      if o.ChannelName |> isblank then
        failwith "No channel name provided"
      let o =
        if o.Machine |> isblank then {o with Machine = Environment.MachineName} else o
      let o =
        if o.JobName |> String.IsNullOrEmpty then
          let jobname =
            if o.ChannelName |> EventJobConfig.IsValidJobName then
              o.ChannelName
            else
              o.ChannelName |> deriveJobName
          if o.AutoJobName then
            if jobname |> EventJobConfig.IsValidJobName |> not then
              cp $"Derived job name '\fr{jobname}\f0' appears to be invalid."
              cp $"To fix, explicitly specify a job name using \fg-job\f0 instead of \fG-J\f0"
              failwith $"Invalid job name '{jobname}'"
            else
              cp $"Using derived job name \fc{jobname}\f0."
              {o with JobName = jobname}
          else
            if jobname |> EventJobConfig.IsValidJobName |> not then
              cp $"No job name specified. \fg-J\f0 would derive the name \fr{jobname}\f0, but that appears to be invalid."
              cp $"To fix, explicitly specify a job name using \fg-job\f0 instead of \fG-J\f0"
              failwith $"No job name specified"
            else
              cp $"No job name specified. \fg-J\f0 would derive the name \fc{jobname}\f0."
              failwith "No job name specified"
        else
          o
      if o.JobName |> EventJobConfig.IsValidJobName |> not then
        failwith $"'{o.JobName}' is not a valid job name"
      let o =
        {o with IsLocal = o.Machine.Equals(Environment.MachineName, StringComparison.InvariantCultureIgnoreCase)}
      o
    | x :: _ ->
      failwith $"Unrecognized argument '{x}'"
  let o = args |> parsemore {
    JobName = null
    AutoJobName = false
    ChannelName = null
    Machine = null
    IsLocal = true
    Admin = false
  }
  let o =
    if o.IsLocal then
      try
        let ers = new EventRecordSource(o.ChannelName)
        cp $"\fc{o.ChannelName}\f0: \fgChannel exists and is accessible.\fb {ers.Info.RecordCount}\f0 records available."
        o
      with
      | :? EventLogNotFoundException as elnfe ->
        cp $"\fc{o.ChannelName}\f0: \frWarning: \foChannel does not exist."
        if o.IsLocal then
          failwith "Local mode channel does not exist."
        o
      | :? UnauthorizedAccessException as uae ->
        cp $"\fc{o.ChannelName}\f0: \foChannel exists, but is not accessible to you.\f0 Setting \fr-admin\f0 flag."
        {o with Admin = true}
    else
      o
  let zone = new EventDataZone(false, o.Machine)
  let byJob = zone.Registry.FindByJob(o.JobName)
  let byChannel = zone.Registry.FindByChannel(o.ChannelName)
  if byJob <> null && byChannel <> null && Object.ReferenceEquals(byJob, byChannel) then
    cp $"\foWarning: Job \fc{o.JobName}\fo for channel \fg{o.ChannelName}\fo already exists\f0 (in machine zone \fb{o.Machine}\f0)."
    cp $"Config file is \fy{zone.JobConfigFile(o.JobName)}\f0"
    1
  elif byJob <> null then
    cp $"\frError: A job named \fc{o.JobName}\fr already exists\f0 (channel \fg{byJob.Channel}\f0) (in machine zone \fb{o.Machine}\f0)."
    cp $"Config file is \fy{zone.JobConfigFile(o.JobName)}\f0"
    2
  elif byChannel <> null then
    cp $"\frError: Channel \fg{o.ChannelName}\fr already has a job attached\f0 (\fc{byChannel.Name}\f0) (in machine zone \fb{o.Machine}\f0)."
    cp $"Config file is \fy{zone.JobConfigFile(byChannel.Name)}\f0"
    3
  else
    cp $"Creating new job \fc{o.JobName}\f0, for channel \fg{o.ChannelName}\f0 in machine zone \fb{o.Machine}\f0."
    let cfg = new EventJobConfig(o.JobName, o.ChannelName, o.Admin)
    let fnm = zone.JobConfigFile(o.JobName)
    cp $"Saving \fy{fnm}\f0"
    zone.WriteConfig(cfg)
    let job = zone.OpenJob(o.JobName)
    cp $"Creating channel DB \fb{job.RawDbFile}\f0"
    job.InitDb()
    0

