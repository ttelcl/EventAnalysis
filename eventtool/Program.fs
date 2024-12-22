// (c) 2022  ttelcl / ttelcl

open System

open ColorPrint
open CommonTools
open ExceptionTool
open Usage

let rec run arglist =
  // For subcommand based apps, split based on subcommand here
  match arglist with
  | "-v" :: rest ->
    verbose <- true
    rest |> run
  | "--help" :: _
  | "-h" :: _ ->
    usage "all"
    0  // program return status code to the operating system; 0 == "OK"
  | [] ->
    usage (if verbose then "all" else "")
    0  // program return status code to the operating system; 0 == "OK"
  | "channels" :: rest ->
    rest |> AppChannels.run
  | "init" :: rest ->
    rest |> JobChannelInit.run
  | "overview1" :: rest ->
    rest |> AppOverview.run
  | "overview" :: rest ->
    rest |> AppOverview2.run
  | "update" :: rest ->
    rest |> AppUpdate.run
  | "jobs" :: rest ->
    rest |> AppJobs.run
  | "samples" :: rest ->
    rest |> AppSamples.run
  | "job-sample" :: rest
  | "jobsample" :: rest ->
    rest |> AppJobSample.run
  | "dump" :: rest ->
    rest |> AppDump.run
  | "plc-dump" :: rest
  | "plcdump" :: rest ->
    rest |> AppPlcDump.run
  | "fix" :: rest ->
    rest |> AppFix.run
  | "metadump" :: rest
  | "meta-dump" :: rest ->
    rest |> AppMetaDump.run
  | "archive" :: rest ->
    rest |> AppArchive.run
  | "diagnose" :: rest
  | "diag" :: rest ->
    rest |> AppDiag.run
  | x :: _ ->
    cp $"\foUnknown command \fr{x}\f0."
    1

[<EntryPoint>]
let main args =
  try
    args |> Array.toList |> run
  with
  | ex ->
    ex |> fancyExceptionPrint verbose
    resetColor ()
    1



