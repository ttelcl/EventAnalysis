module LoggedRun

open System
open System.IO

/// Run "innerfn args", but write stdout to a logfile instead of to
/// console. The logfile is {logfile}.log. If one exists, the log is
/// appended. If one exists but it is larger than 100K, the old one is
/// renamed to a timestamped name, and a new logfile is created.
let runLogged logtag innerfn args =
  let logfilename = Path.Combine(Environment.CurrentDirectory, logtag + ".log")
  let lfi = new FileInfo(logfilename)
  if lfi.Exists && lfi.Length > 40000L then
    // Cycle existing file
    let stamptag = lfi.LastWriteTime.ToString("yyyyMMdd-HHmmss")
    let logold = Path.Combine(Environment.CurrentDirectory, logtag + "." + stamptag + ".log")
    File.Move(logfilename, logold)
  use logfile = File.AppendText(logfilename)
  logfile.WriteLine("---------------------------------------------------------")
  let stamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss K")
  logfile.WriteLine("--- " + stamp)
  let outold = Console.Out
  let ret =
    try
      Console.SetOut(logfile)
      try
        innerfn args
      with
      | ex ->
        logfile.WriteLine()
        logfile.WriteLine("------ ERROR -----")
        logfile.WriteLine(ex)
        1
    finally
      Console.SetOut(outold)
  logfile.WriteLine()
  ret

