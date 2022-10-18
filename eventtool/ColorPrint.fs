module ColorPrint

// (c) 2022 Luc Cluitmans

open System

(*
This module supports writing colored text to the console by interpreting special
"escape" sequences. For simplicity and brevity these are custom for this module,
and not using existing standard codes.

All escape sequences use \f or \v followed by one character to indicate a color
change. [[Side effect: this disables the opportunity to write a formfeed or
vertical tab to the console, but I assumed there are no good reasons to ever do
that...]]

The \f escape indicates a change in foreground color, \v a change in background
color.

Here are the defined escape codes (demonstrated with \f, but \v has equivalent
counterparts). Note that how these colors actually render strongly depends on
your console settings.

\f0 or \v0   Reset both foreground and background colors to their defaults
             (both codes are equivalent)
\fx or \vx   Does nothing. Helpful when aligning different lines where some use
             color escapes and others don't.

\fK          ConsoleColor.Black (0)
\fB          ConsoleColor.DarkBlue (1)
\fG          ConsoleColor.DarkGreen (2)
\fC          ConsoleColor.DarkCyan (3)
\fR          ConsoleColor.DarkRed (4)
\fM          ConsoleColor.DarkMagenta (5)
\fY or \fo   ConsoleColor.DarkYellow (6) (looks orange, hence the 'o')
\fW or \fa   ConsoleColor.Gray (7) ('W' == "Dark White")
\fk or \fA   ConsoleColor.DarkGray (8) ('k' == "Light Black")
\fb          ConsoleColor.Blue (9)
\fg          ConsoleColor.Green (10)
\fc          ConsoleColor.Cyan (11)
\fr          ConsoleColor.Red (12)
\fm          ConsoleColor.Magenta (13)
\fy          ConsoleColor.Yellow (14)
\fw          ConsoleColor.White (15)

*)

(*
Usage examples:

cp "Hello \fggreen\f0 \foworld\f0! (\vRthis is a test\v0)"

To combine string formatting and coloration use one of the following:

* using Printf.kprintf:

  open Printf
  let num = 42
  let s = "Boo!"
  kprintf cp "The answer was \fr%d\f0. \fy%s\f0" num s

* using sprintf:

  let num = 42
  let s = "Boo!"
  (sprintf "The answer was \fr%d\f0. \fy%s\f0" num s) |> cp

*)

type private TextPart =
  | Text of string
  | Foreground of ConsoleColor
  | Background of ConsoleColor
  | Reset
  | Nop

let private colorValue ch =
  match ch with
  | 'K' -> ConsoleColor.Black
  | 'B' -> ConsoleColor.DarkBlue
  | 'G' -> ConsoleColor.DarkGreen
  | 'C' -> ConsoleColor.DarkCyan
  | 'R' -> ConsoleColor.DarkRed
  | 'M' -> ConsoleColor.DarkMagenta
  | 'Y' | 'o' -> ConsoleColor.DarkYellow
  | 'W' | 'a' -> ConsoleColor.Gray
  | 'k' | 'A' -> ConsoleColor.DarkGray
  | 'b' -> ConsoleColor.Blue
  | 'g' -> ConsoleColor.Green
  | 'c' -> ConsoleColor.Cyan
  | 'r' -> ConsoleColor.Red
  | 'm' -> ConsoleColor.Magenta
  | 'y' -> ConsoleColor.Yellow
  | 'w' -> ConsoleColor.White
  | x ->
    x |> failwithf "Unknown color code character: '%c'"

let private colorEscape e ch =
  match ch with
  | '0' -> Reset
  | 'x' -> Nop
  | other ->
    let clr = other |> colorValue
    match e with
    | '\f' -> Foreground(clr)
    | '\v' -> Background(clr)
    | x ->
      failwith "Unrecognized color escape marker"

let private render stderr seglist =
  for seg in seglist do
    match seg with
    | Text(text) ->
      if stderr then
        text |> eprintf "%s"
      else
        text |> printf "%s"
    | Foreground(clr) -> Console.ForegroundColor <- clr
    | Background(clr) -> Console.BackgroundColor <- clr
    | Reset -> Console.ResetColor()
    | Nop -> ()  // do nothing

let private xcolprintEx stderr text =
  let keys = [| '\f'; '\v' |]
  let rec splitMore seglist (text:string) =
    let i = text.IndexOfAny(keys)
    if i < 0 then
      Text(text) :: seglist |> List.rev
    else
      //let dbg = text.Replace('\f', '§').Replace('\v', '¤')
      if text.Length <= i+1 then
        failwith "colprint: the last character of the argument must not be \\f nor \\v"
      let pre = text.Substring(0, i)
      let key = text.[i]
      let code = text.[i+1]
      let rest = text.Substring(i+2)
      let escapePart = colorEscape key code
      rest |> splitMore (escapePart :: Text(pre) :: seglist)
  let segments = text |> splitMore []
  segments |> render stderr

let colprintEx = xcolprintEx false

let ecolprintEx = xcolprintEx true

/// Color-print a string with color escapes to stdout (no line break)
let colprint text =
  text |> colprintEx
  Console.ResetColor()

/// Color-print a line with color escapes to stdout (and a line break)
let colprintn text =
  text |> colprint
  Console.WriteLine()

/// Color-print a string with color escapes to stderr (no line break)
let ecolprint text =
  text |> ecolprintEx
  Console.ResetColor()

/// Color-print a line with color escapes to stderr (and a line break)
let ecolprintn text =
  text |> ecolprint
  Console.Error.WriteLine()

/// Color-print a line with color escapes to stdout (and a line break)
/// (abbreviation for colprintn)
let cp = colprintn

/// Color-print a string with color escapes to stdout (no line break)
/// (abbreviation for colprint)
let cpx = colprint

/// Color-print a line with color escapes to stderr (and a line break)
/// (abbreviation for ecolprintn)
let ecp = ecolprintn

/// Color-print a string with color escapes to stderr (no line break)
/// (abbreviation for ecolprint)
let ecpx = ecolprint

