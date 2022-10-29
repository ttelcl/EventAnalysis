module LoggedRun

open System
open System.IO

let runLogged logtag innerfn args =
  let logfilename = Path.Combine(Environment.CurrentDirectory, logtag + ".log")
  let lfi = new FileInfo(logfilename)
  if lfi.Exists && lfi.Length > 100000L then
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

