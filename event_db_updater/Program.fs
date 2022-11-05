// (c) 2022  ttelcl / ttelcl

open System
open System.Text.RegularExpressions

open CommonTools
open ExceptionTool

(*
  This main module is structured a bit different than usual due
  to the need to write output to a log file instead of to
  console.
*)

type private PreparsedArgs = {
  Tag: string
  Args: string list
  Warnings: string list
}

// Get the tag (if any) out of the argument list. Failure is not an
// option here, because this runs before we have set up the log file.
// The case of an invalid tag causes a string to be added to the
// "Warnings" instead of changing the tag.
let private preparse args =
  let rec parsemore pa args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parsemore pa
    | "-tag" :: tag :: rest ->
      let valid = Regex.IsMatch(tag, @"^([A-Za-z][A-Za-z0-9]*)([-_.][A-Za-z0-9]+)*$")
      if valid then
        rest |> parsemore {pa with Tag = tag}
      else
        let message = $"Ignoring invalid tag '{tag}'"
        rest |> parsemore {pa with Warnings = message :: pa.Warnings}
    | "-tag" :: [] ->
      let message = "Missing argument to '-tag'"
      [] |> parsemore {pa with Warnings = message :: pa.Warnings}
    | x :: rest ->
      rest |> parsemore {pa with Args = x :: pa.Args}
    | [] ->
      {pa with Args = pa.Args |> List.rev; Warnings = pa.Warnings |> List.rev}
  args |> parsemore {
    Tag = "event-db-updater"
    Args = []
    Warnings = []
  }

let private runInner pa =
  if pa.Warnings |> List.isEmpty then
    pa.Args |> AppUpdate.run
  else
    printfn "Run aborted due to argument error(s):"
    for warning in pa.Warnings do
      warning |> printfn "- %s"
    1

let run arglist =
  let pa = arglist |> preparse
  pa |> LoggedRun.runLogged pa.Tag runInner

[<EntryPoint>]
let main args =
  try
    args |> Array.toList |> run
  with // This can only happen if something fails during commandline parsing
  | ex ->
    ex |> fancyExceptionPrint verbose
    resetColor ()
    1



