# Raw event database design, revisited

## Version 2

File name structure: `<job>.raw.sqlite3`

## Table EventId

Now just an index of observed distinct Event IDs

### Columns

|Column|Type|PK|Notes|
|---|---|---|---|
|eid|Integer|*|Event ID|


## Table EventIdEx

Unique combinations of provider name, event ID, task ID,
task name. Note that provider GUIDs are optional!
The expectation is that each event ID appears only once,
but that is not guaranteed!

### Columns

|Column|Type|PK|K2|Notes|
|---|---|---|---|---|
|etp|Integer|*||Row key for this table (autoincrement)|
|eid|Integer||*|Event ID|
|task|Integer||*|Task ID|
|provider|String||*|provider name|
|prvguid|Guid/string|||provider GUID, may be null|
|taskname|string|||task description|

Note that the lookup of the task name may cause an exception
that needs defusing (observed in the "application" log)

## Table Operations

Operation descriptions

### Columns

|Column|Type|PK|K2|Notes|
|---|---|---|---|---|
|ork|Integer|*||Row key for this table|
|etp|Integer||*|Reference to EventIdEx|
|op|Integer||*|operation ID|
|opname|string|||operation description|

## Table EventXml

The raw event XML and nothing more. The key is to have
any interpretation and indexing in separate tables that
just refer to this table.

### Columns

|Column|Type|PK|Notes|
|---|---|---|---|
|rid|Integer|*|Record ID|
|xml|String||The raw XML record|

Note that the XML may occasionally be invalid because of
the presence of control characters. For parsing make sure
to disable character checking!

## Table EventInfo

The standard event information excluding the XML.

### Columns

|Column|Type|PK|Notes|
|---|---|---|---|
|rid|Integer|*|Record ID, PK|
|ork|Integer||Reference to Operations table, implying event, task, operation|
|ts|Integer||Time stamp as ticks since Epoch|
|ver|Integer||Event version|




