module CommonTools

open System
open System.IO
open System.Threading

open ColorPrint

type Color = ConsoleColor
let color clr =
  Console.ForegroundColor <- clr
let bcolor clr =
  Console.BackgroundColor <- clr
let color2 fclr bclr =
  Console.ForegroundColor <- fclr
  Console.BackgroundColor <- bclr

let resetColor () =
  Console.ResetColor()

(*
Typical use of startfile and finishfile:

let filename = "hello-world.txt"
do
  use f = startfile filename
  fprintfn f "Hello world!"
finishfile filename

*)

/// Start writing to a temporary text file whose name is based on the given file name
/// Use 'finishfile' after writing to rename it to its final name
let startFile name =
  let tmp = name + ".tmp"
  name |> sprintf "Writing '\fo%s\f0'" |> cp
  File.CreateText(tmp)

/// Start writing to a temporary binary file whose name is based on the given file name
/// Use 'finishfile' after writing to rename it to its final name
let startFileBinary name =
  let tmp = name + ".tmp"
  name |> sprintf "Writing '\fo%s\f0'" |> cp
  File.Create(tmp)

/// Finish the the file that was written starting from 'startfile' (and create a backup
/// of the previous version if applicable). For scenarios where you do not know the
/// name in advance, use finishFile2 instead.
let finishFile name =
  let tmp = name + ".tmp"
  if File.Exists(name) then
    let bak = name + ".bak"
    File.Replace(tmp, name, bak)
  else
    File.Move(tmp, name)

/// Finish the the file that was written starting from 'startfile' named
/// using the given 'scratchName' (scratchName.tmp), and rename it to the given
/// final name 'trueName' (creating a backup trueName.bak if necessary).
/// This function helps in scenarios where the final name can be derived only
/// after writing.
let finishFile2 trueName scratchName =
  let tmp = scratchName + ".tmp"
  if File.Exists(trueName) then
    let bak = trueName + ".bak"
    File.Replace(tmp, trueName, bak)
  else
    File.Move(tmp, trueName)

/// General purpose mutable verbosity flag
let mutable verbose = false

/// Generic argument list splitter: turns a list of strings
/// into a list of lists of strings where the first string starts with a '-'
let splitArgs args =
  let rec cleanAndReverse lo l =
    match l with
    | [] :: rest ->
      rest |> cleanAndReverse lo
    | x :: rest ->
      rest |> cleanAndReverse (x :: lo)
    | [] ->
      lo
  let rec splitInner ll l (rest:string list) =
    match rest with
    | o :: tail when o.StartsWith("-") ->
      let lr = l |> List.rev
      tail |> splitInner (lr :: ll) [o]
    | a :: tail ->
      tail |> splitInner ll (a :: l)
    | [] ->
      let lr = l |> List.rev
      let lx = lr :: ll
      lx |> cleanAndReverse []
  args |> splitInner [] []

/// Split a list of strings into two lists, the first of which
/// will have no items starting with "-" except possibly the first
/// element
let splitNoDash args =
  let rec splitMore l1 (l2: string list) =
    match l2 with
    | x :: rest when not(x.StartsWith("-")) ->
      splitMore (x :: l1) rest
    | [] ->
      (l1 |> List.rev), l2
    | rest ->
      (l1 |> List.rev), rest
  match args with
  | x :: rest ->
    splitMore [x] rest
  | [] ->
    [], []

let private consoleCancelToken =
  let cts = new CancellationTokenSource()
  let token = cts.Token
  Console.CancelKeyPress.Add(
      fun args ->
        if token.IsCancellationRequested then
          color ConsoleColor.Red
          printfn "Second CTRL-C: aborting"
          resetColor ()
        else
          color ConsoleColor.Yellow
          printfn "Stopping ...!"
          resetColor ()
          printfn "(stop in progress)"
          cts.Cancel()
          args.Cancel <- true
      )
  token

/// A function that returns true after CTRL-C has been pressed once
/// (Pressing CTRL-C again has the usual behaviour of immediately
/// aborting the program.)
let canceled =
  fun () -> consoleCancelToken.IsCancellationRequested

