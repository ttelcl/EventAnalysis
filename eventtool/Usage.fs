// (c) 2022  ttelcl / ttelcl
module Usage

open System

open CommonTools
open ColorPrint

let usage targetCommand =
  let all =
    StringComparer.OrdinalIgnoreCase.Compare(targetCommand, "all") = 0
    || targetCommand = ""
  let targetMatch key =
    all
    || StringComparer.OrdinalIgnoreCase.Compare(targetCommand, key) = 0
  let detailed = targetCommand <> ""
  if all then
    cp "\foWindows Event Log utility\f0"
    cp "\fyCommon options\f0:"
    cp "    \fg-v           \f0Verbose mode"
    cp "    \fg-h           \f0Print help message, with extra detail for the current command (or all commands)"
  if targetMatch "jobs" then
    cp "\foeventtool \fyjobs\f0 [\fg-m \fc<machine>\f0|\fg-M\f0]"
    cp "   List jobs in the current machine zone, or the specified machine zone (\fg-m\f0), or all machine zones (\fg-M\f0)."
    if detailed then
      ()
  if targetMatch "channels" then
    cp "\foeventtool \fychannels\f0 [\fg-save \fc<filename>\f0]"
    cp "   Print or save a list of known event log channel names on this computer."
    if detailed then
      ()
  if targetMatch "overview" then
    cp "\foeventtool \fyoverview\f0 \fg-job \fc<jobname>\f0 [\fg-m \fc<machine>\f0] [\fg-counts\f0]"
    cp "   Print provider, event, task and opcode info for the channel. Optionally include event counts."
    if detailed then
      ()
  if targetMatch "init" then
    cp "\foeventtool \fyinit\f0 \fg-channel \fc<channel>\f0 [\fg-job \fc<jobname>\f0|\fg-J\f0] [\fg-admin\f0] [\fg-m \fc<machine>\f0]"
    cp "   Create a new event channel job (configuration file, folder structure, empty database)."
    if detailed then
      cp "   \fg-channel \fc<channel>\f0   The event log channel name"
      cp "   \fg-job \fc<jobname>\f0       The alias used to identify the channel."
      cp "   \fg-J\fx\f0                   Derive a job name from the channel name."
  if targetMatch "samples" then
    cp "\foeventtool \fysamples\f0 \fg-job \fc<jobname>\f0 [\fg-p \fc<provider>\f0] \fg-e \fc<event-id>\f0 [\fg-n \fc<n>\f0] [\fg-m \fc<machine>\f0]"
    cp "   Extract sample events from a store (using V2 database)"
    if detailed then
      cp "   \fg-job \fc<jobname>\f0       The name of a job or channel to extract events from"
      cp "   \fg-n \fc<n>\f0               The number of events to extract. Default 2. The events are smoothly spread"
      cp "   \fx\fx\fx                     (so: the default of 2 samples will extract the first and last matching event)"
      cp "   \fg-e \fc<event-id>\f0        The event ID. In case multiple providers provide the same event, a \fg-p\f0 option is required."
      cp "   \fg-p \fc<provider>\f0        Disambiguate the event source. \fc<provider>\f0 can be a provider ID or a unique part of"
      cp "   \fx\fx\fx                     the provider name. Use \foeventtool \fyoverview\f0 to discover provider names for an event."
  if targetMatch "job-sample" then
    cp "\foeventtool \fyjob-sample\f0 \fg-job \fc<jobname>\f0 [\fg-n \fc<n>\f0] [\fg-m \fc<machine>\f0]"
    cp "   Create a sample dump XML file containing a sample for each distinct provider/event combination"
    if detailed then
      cp "   \fg-job \fc<jobname>\f0       The name of a job or channel to extract events from"
      cp "   \fg-n\f0\fx                   The number of events to include. This can be \fb0\f0 to not include any event samples."
  if targetMatch "update" then
    cp "\foeventtool \fyupdate\f0 {\fg-job \fc<jobname>\f0} \fg-cap \fc<n>\f0 [\fg-db1\f0] [\fg-db2\f0]"
    cp "   Run one or more jobs, transferring events from the event log channel into the job's DB."
    if detailed then
      cp "   \fg-job \fc<jobname>\f0       The name of a job or channel"
      cp "   \fg-cap \fc<n>\f0             The maximum number of events to copy"
      cp "   \fg-db1 \fx   \f0             Update the legacy (V1) database"
      cp "   \fg-db2 \fx   \f0             Update the new (V2) database"
  if targetMatch "metadump" then
    cp "\foeventtool \fymetadump\f0 \fg-job \fc<jobname>\f0 [\fg-m \fc<machine>\f0]"
    cp "   Dump the current metadata for the job in JSON form: all DB content excluding actual events"
    if detailed then
      cp "   \fg-job \fc<jobname>\f0       The name of the job (selecting the database to dump)"
      cp "   \fg-m \fc<machine>\f0         The machine whose events to inspect (default: current machine)"
  if targetMatch "dump" then
    cp "\foeventtool \fydump\f0 [\fg-list\f0] \fg-job \fc<jobname>\f0 \fg-e \fc<event>\f0 [\fg-p \fc<provider>\f0] [\fg-n \fc<n>\f0|\fg-N\f0] [\fg-to \fc<rid>\f0]"
    cp "   Dump data for matching events to a CSV file. Events are processed from newest to oldest."
    if detailed then
      cp "   \fg-list\fx\fx\f0             Instead of dumping, just list the fields found in the first record"
      cp "   \fg-job \fc<jobname>\f0       The name of a job or channel"
      cp "   \fg-e \fc<event>\f0           The ID of the event to dump"
      cp "   \fg-p \fc<provider>\f0        The id or (partial) name of the event provider to disambiguate the event ID"
      cp "   \fg-n \fc<n>\f0               The maximum number of events to dump (default 1000)"
      cp "   \fg-N \fx\fx                  Remove the cap on the number of events (equivalent to \fg-n \fc2147483647\f0)"
      cp "   \fg-to \fc<rid>\f0            Enumerate events going back from record id \fc<rid>\f0 (default: last known RID)"
  if targetMatch "archive" then
    cp "\foeventtool \fyarchive\f0 \fm...\f0"
    cp "   Archive database operations \frWork In Progress\f0!"
    if detailed then
      cp "   \fg-job \fc<jobname>\f0       The name of the job (selecting the database)"
      cp "   \fg-m \fc<machine>\f0         The machine whose events to inspect (default: current machine)"
  if targetMatch "diag" then
    cp "\foeventtool \fydiag\f0 \fg-job \fc<jobname>\f0 [\fg-m \fc<machine>\f0] [\fg-day\f0|\fg-week\f0|\fg-month\f0] [\fg-rid \fc<ridmin>\f0]"
    cp "   Generate a CSV file summarizing DB content on a day-by-day or month-by-month base"
    if detailed then
      cp "   \fg-job \fc<jobname>\f0       The name of the job (selecting the database)"
      cp "   \fg-m \fc<machine>\f0         The machine whose events to inspect (default: current machine)"
      cp "   \fg-rid \fc<ridmin>\f0        The RID where to start scanning"
  if targetMatch "fix" then
    cp "\foeventtool \fyfix\f0 [\fg-job \fc<jobname>\f0|\fg-all\f0] [\fg-m \fc<machine>\f0]"
    cp "   Create missing database files for the job."
  if targetMatch "export" then
    cp "\foeventtool \fyexport\f0 [\fg-m \fc<machine>\f0] \fg-job \fc<jobname> \fg-file \fc<dumpfile>"
    cp "   \frNot yet implemented!\f0 Export events from an event job database to an event data file."
    if detailed then
      ()
  if targetMatch "import" then
    cp "\foeventtool \fyimport\f0 \fg-m \fc<machine>\f0 \fg-job \fc<jobname> \fg-file \fc<dumpfile>"
    cp "   \frNot yet implemented!\f0 Import events from an event data file into an event job database."
    if detailed then
      ()
  if targetMatch "plc-dump" then
    cp "\fR(legacy) \foeventtool \fyplc-dump\f0 [\fg-from \fc<rid>\f0] [\fg-to \fc<rid>\f0] [\fg-job \fc<job>\f0 {\fg-e \fc<eid>\f0}] [\fg-m \fc<machine>\f0]"
    cp "   \fkBackward compat event dump file export (if -job and -e are omitted)\f0"
  if targetMatch "overview1" then
    cp "\fR(legacy) \foeventtool \fyoverview1\f0 \fg-job \fc<jobname>\f0 [\fg-m \fc<machine>\f0] [\fg-nosize\f0]"
    cp "   \fkPrint event and task statistics and settings for the channel, using the \fRlegacy\fk data\f0."
    if detailed then
      ()
  
  


