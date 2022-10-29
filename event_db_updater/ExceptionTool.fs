module ExceptionTool

(*
  Provides functionality for displaying exception messages in detailed
  or brief styles.
*)

open System
open System.Diagnostics
open System.IO

open CommonTools

(*
  This module has not yet been converted to use the newer and more compact
  functionality in the ColorPrint module for color display, but uses the
  older 'color' and 'resetColor' functions instead.
*)

let rec fancyExceptionPrint showTrace (ex:Exception) =
  try
    color Color.Red
    printf "%s" (ex.GetType().FullName)
    resetColor()
    printf ": "
    color Color.DarkYellow
    printf "%s" ex.Message
    resetColor()
    printfn ""
    if showTrace then
      let trace = new StackTrace(ex, true)
      for frame in trace.GetFrames() do
        printf "  "
        let fnm =
          if frame.HasSource() then
            let fnm = frame.GetFileName()
            color Color.Red
            printf "%15s" (Path.GetFileName(fnm))
            resetColor()
            printf ":"
            color Color.Green
            printf "%4d" (frame.GetFileLineNumber())
            resetColor()
            fnm
          else
            color Color.Red
            printf "%15s" "?"
            resetColor()
            printf ":"
            printf "    "
            resetColor()
            null
        if frame.HasMethod() then
          let method = frame.GetMethod()
          printf " "
          color Color.Yellow
          printf "%s" (method.Name)
          printf "("
          let pinfs = method.GetParameters()
          if pinfs.Length>0 then
            color Color.DarkYellow
            printf "[%d]" pinfs.Length
            color Color.Yellow
            printf ")"
          else
            printf ")"
          color Color.DarkGray
          printf " "
          color Color.White
          printf "%s" (method.ReflectedType.Name)
          resetColor()
        else
          color Color.Red
          printf "(?)"
          resetColor()
        if fnm <> null then
          color Color.DarkGray
          printf " (%s)" (Path.GetDirectoryName(fnm))
          resetColor()
        printfn ""
      ()
    finally
      resetColor()
  if ex.InnerException <> null then
    color Color.Cyan
    printf "----> "
    resetColor()
    ex.InnerException |> fancyExceptionPrint showTrace


