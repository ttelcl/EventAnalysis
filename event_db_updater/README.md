# event_db_updater

A pseudo-CLI app doing almost the same as "eventtool update".

Any "console" output is not written to the console but to a log file.
While the app is written in a CLI app style it is actually a windows
app, so it does not spawn a console.

Usage:

```text
event_db_updater [-tag <tag>] [-cap <n>] {-job <jobname>}
```

The output will be written to a log file in the _**current directory**_
with a name derived from &lt;tag&gt;.	If the previous log file is
too large, it is renamed to a timestamped name first, otherwise the
new log is appended.

Options:

`-v` Write more verbose messages to the log file (in particular:
full stacktraces in case of error)

`-tag <tag>` The tag to derive the log file name from. Default
_event-db-generator_

`-cap <n>` The maximum number of records to copy per invocation 
(default 20000)

`-job <jobname>` The name of the update jobs to run. At least one
is required, but there can be multiple (see eventtool help)
