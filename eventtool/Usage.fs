// (c) 2022  ttelcl / ttelcl
module Usage

open CommonTools
open ColorPrint

let usage detailed =
  cp "\foWindows Event Log utility\f0"
  cp ""
  cp "\foeventtool \fylist\f0 [\fg-m \fc<machine>\f0|\fg-M\f0]"
  cp "   List jobs in the data zone for the given machine (or all data zones)."
  cp "\foeventtool \fychannels\f0 [\fg-save \fc<filename>\f0]"
  cp "   Print a list of known event log channel names on this computer."
  cp "\foeventtool \fyinit\f0 \fg-channel \fc<channel>\f0 [\fg-job \fc<jobname>\f0] [\fg-admin\f0] [\fg-m \fc<machine>\f0]"
  cp "   Create a new event channel job. \fc<jobname>\f0 is the alias used to identify the channel."
  cp "   \fg-channel \fc<channel>\f0   The event log channel name"
  cp "   \fg-job \fc<jobname>\f0       The alias used to identify the channel."
  cp "\foeventtool \fystatus\f0 \fg-job \fc<jobname>"
  cp "   Print event and task statistics and settings for the channel."
  cp "\foeventtool \fydisable\f0 \fg-job \fc<jobname>\f0 {\fg-e \fc<event-id>\f0}"
  cp "   Disable import of one or more event types for a channel."
  cp "\foeventtool \fyenable\f0 \fg-job \fc<jobname>\f0 {\fg-e \fc<event-id>\f0}"
  cp "   Re-enable import of one or more event types for a channel."
  cp "\foeventtool \fyupdate\f0 \fg-job \fc<jobname>"
  cp "   Run an event job, transferring events from the event log channel into the job's DB."
  cp "\foeventtool \fyexport\f0 [\fg-m \fc<machine>\f0] \fg-job \fc<jobname> \fg-file \fc<dumpfile>"
  cp "   Export events from an event job database to an event data file."
  cp "\foeventtool \fyimport\f0 \fg-m \fc<machine>\f0 \fg-job \fc<jobname> \fg-file \fc<dumpfile>"
  cp "   Import events from an event data file into an event job database."
  cp "\fg-v               \f0Verbose mode"



