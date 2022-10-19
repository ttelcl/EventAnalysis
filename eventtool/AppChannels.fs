module AppChannels

open System
open System.Diagnostics.Eventing.Reader

open ColorPrint
open CommonTools

type private Options = {
  SaveFile: string
}

let run args =
  let rec parsemore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parsemore o
    | "-save" :: fnm :: rest ->
      rest |> parsemore {o with SaveFile = fnm}
    | [] ->
      o
    | x :: _ ->
      failwith $"Unrecognized argument at '{x}'"
  let o = args |> parsemore {
    SaveFile = null
  }
  let names =
    EventLogSession.GlobalSession.GetLogNames()
    |> Seq.sort
    |> Seq.toArray
  if o.SaveFile |> String.IsNullOrEmpty then
    // colorize the names a bit
    for name in names do
      let lst = name.Split('/') |> Seq.toList
      let rec printsegments segments =
        match segments with
        | last :: [] ->
          last |> sprintf "\fg%s\f0" |> cp
        | segment :: rest ->
          segment |> sprintf "\fc%s\f0/" |> cpx
          rest |> printsegments
        | [] ->
          // do nothing at all, ignore the emoty line
          ()
      lst |> printsegments
  else
    cp $"Saving \fy{names.Length}\f0 log channel names to \fg{o.SaveFile}\f0."
    do
      use w = o.SaveFile |> startFile
      for name in names do
        w.WriteLine(name)
    o.SaveFile |> finishFile
  0

