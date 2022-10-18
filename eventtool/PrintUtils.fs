module PrintUtils

open CommonTools

(*
  This is an older color printing helper module.
  Consider using the functionality provided by the ColorPrint module instead
*)

/// prints a line in a color
let pclr clr s =
  color clr
  printfn "%s" s
  resetColor()
  
/// prints a line in two colors, splitting the line on the first occurrence of two consecutive spaces
let pclr2 c1 c2 (s:string) =
  let idx = s.IndexOf("  ")
  let s1, s2 =
    if idx<0 then
      s, ""
    else
      s.Substring(0, idx), s.Substring(idx)
  color c1
  printf "%s" s1
  color c2
  printfn "%s" s2
  resetColor()
  
/// prints a line in dark yellow (orange)
let py = pclr Color.DarkYellow

/// prints a line in gray
let pn = pclr Color.Gray

/// prints a two-color line in green and gray (separated by the first occurrence of two spaces)
let p2 = pclr2 Color.Green Color.Gray

let ph (s:string) =
  printfn ""
  let parts = s.Split([| ' ' |], 3)
  color Color.DarkYellow
  printf "%s" parts.[0]
  if parts.Length>1 then
    printf " "
    color Color.Blue
    printf "%s" parts.[1]
    resetColor()
  if parts.Length>2 then
    color Color.DarkYellow
    printf " %s" parts.[2]
  resetColor()
  printfn ""



