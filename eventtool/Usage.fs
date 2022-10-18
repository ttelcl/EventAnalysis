// (c) 2022  ttelcl / ttelcl
module Usage

open CommonTools
open ColorPrint

let usage detailed =
  cp "\foWindows Event Log utility\f0"
  cp ""
  cp "\foeventtool \fylist\f0 [\fg-m \fc<machine>\f0|\fg-M\f0]"
  cp "   List jobs in the data zone for the given machine (or all data zones)."
  cp "\foeventtool \fyloglist\f0"
  cp "   Print a list of known event log names on this computer."
  cp "\foeventtool \fyinit\f0 \fg-job \fc<jobname>\f0 [\fg-m \fc<machine>\f0|\fg-log \fc<logname>\f0] [\fg-admin\f0]"
  cp "   Create a new event export job."
  cp "\foeventtool \fystatus\f0 \fg-job \fc<jobname>"
  cp "   Print event and task statistics and settings for the log."
  cp "\foeventtool \fydisable\f0 \fg-job \fc<jobname>\f0 {\fg-e \fc<event-id>\f0}"
  cp "   Disable import of one or more event types for a job."
  cp "\foeventtool \fyenable\f0 \fg-job \fc<jobname>\f0 {\fg-e \fc<event-id>\f0}"
  cp "   Re-enable import of one or more event types for a job."
  cp "\foeventtool \fyupdate\f0 \fg-job \fc<jobname>"
  cp "   Run an event job, transferring events from the event log into the job's DB."
  cp "\foeventtool \fyexport\f0 [\fg-m \fc<machine>\f0] \fg-job \fc<jobname> \fg-file \fc<file.xmll.gz>"
  cp "   Export events from an event job database to an event data file."
  cp "\foeventtool \fyimport\f0 \fg-m \fc<machine>\f0 \fg-job \fc<jobname> \fg-file \fc<file.xmll.gz>"
  cp "   Import events from an event data file into an event job database."
  cp "\fg-v               \f0Verbose mode"



