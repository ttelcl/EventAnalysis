// (c) 2022  ttelcl / ttelcl
module Usage

open CommonTools
open ColorPrint

let usage detailed =
  cp "\foWindows Event Log utility\f0"
  cp ""
  cp "\foeventtool \fyjobs\f0 [\fg-m \fc<machine>\f0|\fg-M\f0]"
  cp "   List jobs in the current machine zone, or the specified machine zone (\fg-m\f0), or all machine zones (\fg-M\f0)."
  cp "\foeventtool \fychannels\f0 [\fg-save \fc<filename>\f0]"
  cp "   Print or save a list of known event log channel names on this computer."
  cp "\foeventtool \fyinit\f0 \fg-channel \fc<channel>\f0 [\fg-job \fc<jobname>\f0|\fg-J\f0] [\fg-admin\f0] [\fg-m \fc<machine>\f0]"
  cp "   Create a new event channel job (configuration file, folder structure, empty database)."
  cp "   \fg-channel \fc<channel>\f0   The event log channel name"
  cp "   \fg-job \fc<jobname>\f0       The alias used to identify the channel."
  cp "   \fg-J\fx\f0                   Derive a job name from the channel name."
  cp "\foeventtool \fyupdate\f0 {\fg-job \fc<jobname>\f0} \fg-cap \fc<n>\f0 [-q\f0]"
  cp "   Run one or more jobs, transferring events from the event log channel into the job's DB."
  cp "   \fg-job \fc<jobname>\f0       The name of a job or channel"
  cp "   \fg-cap \fc<n>\f0             The maximum number of events to copy"
  cp "   \fg-q\fx\f0                   Quiet mode: don't print anything if the operation succeeded."
  cp "\foeventtool \fyoverview\f0 \fg-job \fc<jobname>\f0 [\fg-save\f0] [\fg-m \fc<machine>\f0]"
  cp "   Print / save event and task statistics and settings for the channel."
  cp "\foeventtool \fydisable\f0 \fg-job \fc<jobname>\f0 {\fg-e \fc<event-id>\f0}"
  cp "   Disable import of one or more event types for a channel."
  cp "\foeventtool \fyenable\f0 \fg-job \fc<jobname>\f0 {\fg-e \fc<event-id>\f0}"
  cp "   Re-enable import of one or more event types for a channel."
  cp "\foeventtool \fyexport\f0 [\fg-m \fc<machine>\f0] \fg-job \fc<jobname> \fg-file \fc<dumpfile>"
  cp "   Export events from an event job database to an event data file."
  cp "\foeventtool \fyimport\f0 \fg-m \fc<machine>\f0 \fg-job \fc<jobname> \fg-file \fc<dumpfile>"
  cp "   Import events from an event data file into an event job database."
  cp "\fyCommon options\f0:"
  cp "    \fg-v           \f0Verbose mode"



