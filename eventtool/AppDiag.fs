module AppDiag

open System
open System.Globalization

open XsvLib
open XsvLib.Buffers

open Lcl.EventLog.Jobs
open Lcl.EventLog.Jobs.Database
open Lcl.EventLog.Utilities

open ColorPrint
open CommonTools
open System.IO

type private DiagMode =
  | Day
  | Week
  | Month

type private DiagOptions = {
  Period: DiagMode option
  JobName: string
  MachineName: string
  RidMin: int64
}

let private periodKey period (time: DateTimeOffset) =
  match period with
  | DiagMode.Day ->
    time.UtcDateTime.ToString("yyyy-MM-dd")
  | DiagMode.Week ->
    let year = ISOWeek.GetYear(time.UtcDateTime)
    let week = ISOWeek.GetWeekOfYear(time.UtcDateTime)
    $"{year:D4}-W{week:D2}"
  | DiagMode.Month ->
    time.UtcDateTime.ToString("yyyy-MM")

let formatStamp et =
  (et |> TimeUtil.EpochDateTimeOffset).ToString("yyyyMMdd-HHmmss-fff")

let private runDiagInner o (odb: OpenDbV2) (row0: EventHeaderRow) =
  let xbuffer = new XsvBuffer(true)
  let colPeriod = "period" |> xbuffer.Declare<string>
  let colRidMin = "ridmin" |> xbuffer.Declare<int64>
  let colRidMax = "ridmax" |> xbuffer.Declare<int64>
  let colCount = "count" |> xbuffer.Declare<int>
  let colTotal = "total" |> xbuffer.Declare<int>
  let colTotalM = "total(M)" |> xbuffer.Declare<string>
  let colMaxBytes = "maxBytes" |> xbuffer.Declare<int>
  let colAvgBytes = "avgBytes" |> xbuffer.Declare<int>
  let colUtcMin = "utcmin" |> xbuffer.Declare<string>
  let colUtcMax = "utcmax" |> xbuffer.Declare<string>
  xbuffer.Lock()
  let period = o.Period.Value
  let lastRow = odb.LastEventHeader() // we can be sure this isn't null
  let key0 = (row0.Stamp |> TimeUtil.EpochDateTimeOffset).UtcDateTime.ToString("yyyyMMdd-HH")
  let key1 = (lastRow.Stamp |> TimeUtil.EpochDateTimeOffset).UtcDateTime.ToString("yyyyMMdd-HH")
  let onm0 = $"{o.JobName}.diag-{period.ToString().ToLower()}.{key0}.{key1}.csv"
  let folder = odb.Owner.DbDirectory
  let onm = Path.Combine(folder, onm0)
  do
    // cp $"Writing \fg{onm}\f0"
    use w = onm |> startFile
    let itrw = Xsv.WriteXsv(w, onm, xbuffer.Count)
    xbuffer.WriteHeader(itrw)
    let mutable stop = false
    let stats = new EventRowStatistics()
    // key0 |> stats.Reset // not needed
    let emitAndReset newTag =
      if stats.Count > 0 then
        stats.Tag |> colPeriod.Set
        stats.MinRid |> colRidMin.Set
        stats.MaxRid |> colRidMax.Set
        stats.Count |> colCount.Set
        stats.TotalSize |> colTotal.Set
        (float(stats.TotalSize) / 1000000.0).ToString("F6") |> colTotalM.Set
        stats.StampMin |> formatStamp |> colUtcMin.Set
        stats.StampMax |> formatStamp |> colUtcMax.Set
        stats.MaxSize |> colMaxBytes.Set
        stats.TotalSize / stats.Count |> colAvgBytes.Set
        itrw |> xbuffer.WriteRow
      newTag |> stats.Reset
    let observe (row: EventViewRow) =
      let rowKey = row.Stamp |> TimeUtil.EpochDateTimeOffset |> periodKey period
      if rowKey <> stats.Tag then
        rowKey |> emitAndReset
        cpx $"\r\fg{rowKey}\f0...     "
        if canceled() then
          stop <- true
      row |> stats.ObserveRow
      stop |> not
    
    odb.ChunkedEvents(32768, false, new Nullable<int>(), o.RidMin)
    |> Seq.takeWhile observe
    |> Seq.iter ignore
    "" |> emitAndReset

  onm |> finishFile
  0

let private runDiag o =
  let ezc = new EventZoneConfig(o.MachineName)
  let edz = new EventDataZone(true, ezc.Machine)
  if edz.Exists |> not then
    cp $"\frNo data for machine \f0'\fc{ezc.Machine}\f0'"
    1
  else
    let job = edz.TryOpenJob(o.JobName)
    if job = null then
      cp $"\frNo job or channel \f0'\fg{o.JobName}\f0' \frknown for machine \f0'\fc{o.MachineName}\f0'"
      1
    else
      if job.HasDbV2 |> not then
        cp $"\foNo data recorded yet for job \f0'\fg{o.JobName}\f0'"
        1
      else
        use odb = job.OpenInnerDatabase2(false)
        let lastHeader = odb.LastEventHeader()
        if lastHeader = null then
          cp $"\foNo data recorded yet for job \f0'\fg{o.JobName}\f0' (DB is empty)"
          1
        elif lastHeader.RecordId < o.RidMin then
          cp $"\foNo relevant data for job \f0'\fg{o.JobName}\f0' (last RID is \fb{lastHeader.RecordId}\f0)"
          1
        else
          let firstHeader =
            odb.QueryEventHeaders(ridMin=o.RidMin, reverse=false, limit=1)
            |> Seq.head // we can be sure that row exists, since the "last" row will match if nothing else
          let dtoFirst = firstHeader.Stamp |> TimeUtil.EpochDateTimeOffset
          let pFirst = dtoFirst |> periodKey o.Period.Value
          let dtoLast = lastHeader.Stamp |> TimeUtil.EpochDateTimeOffset
          let pLast = dtoLast |> periodKey o.Period.Value
          cpx $"RID range is \fb{firstHeader.RecordId}\f0 - \fb{lastHeader.RecordId}\f0."
          cp $" Period range is \fg{pFirst}\f0 - \fg{pLast}\f0."
          runDiagInner o odb firstHeader

let run args =
  let rec parsemore o args =
    match args with
    | "-v" :: rest ->
      verbose <- true
      rest |> parsemore o
    | "-h" :: _ ->
      None
    | "-job" :: jobname :: rest ->
      rest |> parsemore {o with JobName = jobname}
    | "-machine" :: mnm :: rest
    | "-m" :: mnm :: rest ->
      rest |> parsemore {o with MachineName = mnm}
    | "-day" :: rest ->
      rest |> parsemore {o with Period = DiagMode.Day |> Some}
    | "-week" :: rest ->
      rest |> parsemore {o with Period = DiagMode.Week |> Some}
    | "-month" :: rest ->
      rest |> parsemore {o with Period = DiagMode.Month |> Some}
    | "-rid" :: ridtxt :: rest
    | "-ridmin" :: ridtxt :: rest ->
      let ok, rid = ridtxt |> Int64.TryParse
      if ok then
        rest |> parsemore {o with RidMin = rid }
      else
        cp "\foExpecting an integer after \fg-rid\f0!"
        None
    | [] ->
      if o.JobName |> String.IsNullOrEmpty then
        cp "\foNo \fg-job\fo name specified\f0"
        None
      elif o.Period.IsNone then
        cp "\foNo mode (\fg-day\fo, \fg-week\f0, or \fg-month\fo) specified\f0."
        None
      else
        o |> Some
    | x :: _ ->
      cp $"\foUnrecognized argument \fy{x}\f0."
      None
  let oo = args |> parsemore {
    Period = None
    JobName = null
    MachineName = Environment.MachineName
    RidMin = 1
  }
  match oo with
  | None ->
    Usage.usage "diag"
    1
  | Some(o) ->
    o |> runDiag

