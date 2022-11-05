# Raw event database design

## Version 1

File name structure: `<job>.raw-events.sqlite3`

This design has the following tables

### Table EventState

Intended to configure an Event filter: stores for each Event ID
a minimum supported version and an enable / disable flag.

#### Issues

* Not really used in practice, beyond being a convenient index of known
event IDs

#### Columns

|Column|Type|PK|Notes|
|---|---|---|---|
|eid|Integer|*|Event ID|
|minversion|Integer||Minimal version|
|enabled|0 or 1||Enable flag|

### Table Tasks

Maps each Event ID + Task ID to a task description, if known.

#### Issues

While sufficient for many logs, task + event isn't always enough to 
uniquely identify a role ... I think. (Application log seems to have
many sources, but doesn't behave as expected anyway). Sometimes
Operation ID and description is relevant too.

#### Columns

|Column|Type|PK|Notes|
|---|---|---|---|
|eid|Integer|*|Event ID|
|task|Integer|*|Task ID|
|description|String||Task description, if available|


### Table Events

The main raw event storage, containing both basic fields plus the
full raw XML blob.

#### Issues

* Operations that don't need the XML blob are slow, because they still
need to load the entire row, including the relatively large XML blob
into memory.
* Some fields that on second thought may be relevant are missing: 
Operation (and name), Provider Name & GUID

#### Columns

|Column|Type|PK|Notes|
|---|---|---|---|
|rid|Integer|*|Record ID, PK|
|eid|Integer||Event ID|
|task|Integer||Task ID|
|ts|Integer||Time stamp as ticks since Epoch|
|ver|Integer||Event version|
|xml|String||The XML blob|




