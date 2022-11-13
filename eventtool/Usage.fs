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
  if targetMatch "fix" then
    cp "\foeventtool \fyfix\f0 [\fg-job \fc<jobname>\f0|\fg-all\f0] [\fg-m \fc<machine>\f0]"
    cp "   Create missing database files for the job."
  if targetMatch "update" then
    cp "\foeventtool \fyupdate\f0 {\fg-job \fc<jobname>\f0} \fg-cap \fc<n>\f0 [\fg-db1\f0] [\fg-db2\f0]"
    cp "   Run one or more jobs, transferring events from the event log channel into the job's DB."
    if detailed then
      cp "   \fg-job \fc<jobname>\f0       The name of a job or channel"
      cp "   \fg-cap \fc<n>\f0             The maximum number of events to copy"
      cp "   \fg-db1 \fx   \f0             Update the legacy (V1) database"
      cp "   \fg-db2 \fx   \f0             Update the new (V2) database"
  if targetMatch "plc-dump" then
    cp "\foeventtool \fyplc-dump\f0 [\fg-from \fc<rid>\f0] [\fg-to \fc<rid>\f0] [\fg-job \fc<job>\f0 {\fg-e \fc<eid>\f0}] [\fg-m \fc<machine>\f0]"
    cp "   Backward compat event dump file export (if -job and -e are omitted)"
  if targetMatch "samples" then
    cp "\foeventtool \fysamples\f0 \fg-job \fc<jobname>\f0 \fg-e \fc<event-id>\f0 [\fg-n \fc<n>\f0] [\fg-m \fc<machine>\f0]"
    cp "   Extract sample events from a store"
    if detailed then
      cp "   \fg-job \fc<jobname>\f0       The name of a job or channel to extract events from"
      cp "   \fg-n \fc<n>\f0               The number of events to extract. Default 2. The events are smoothly spread."
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
  if targetMatch "overview1" then
    cp "\fR(legacy) \foeventtool \fyoverview1\f0 \fg-job \fc<jobname>\f0 [\fg-m \fc<machine>\f0] [\fg-nosize\f0]"
    cp "   \fkPrint event and task statistics and settings for the channel, using the \fRlegacy\fk data\f0."
    if detailed then
      ()
  
  


