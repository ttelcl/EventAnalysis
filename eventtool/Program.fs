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
  | "-h" :: _
  | [] ->
    usage verbose
    0  // program return status code to the operating system; 0 == "OK"
  | "channels" :: rest ->
    rest |> AppChannels.run
  | "init" :: rest ->
    rest |> JobChannelInit.run
  | "update" :: rest ->
    rest |> AppUpdate.run
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



