# Raw event database design, revisited

## Version 2

File name structure: `<job>.raw.sqlite3`.

----

## Table "ProviderInfo"

Lookup for observed event providers.

### Columns

|Column|Type|PK|K2|Notes|
|---|---|---|---|---|
|prvid|integer|*||row ID|
|prvname|string||*|provider name|
|prvguid|string|||Nullable, provider guid|

----

## Table "TaskInfo"

Maps each unique combination of Event ID, Event Version, 
Task ID, and Provider to a task description. The task description
is null if not yet known, an empty string if digging it up failed
(and is expected to fail again)
The expectation is that each event ID appears only once,
but that is not guaranteed.

### Columns

|Column|Type|PK|K2|Notes|
|---|---|---|---|---|
|eid|Integer||*|Event ID|
|ever|Integer||*|Event version|
|task|Integer||*|Task ID|
|prvid|Integer||*|Provider ID (ref to ProviderInfo)|
|taskdesc|string|||task description|

Note that the source lookup of the task name may cause an exception
that needs defusing (observed in the "application" log)

----

## Table "OperationInfo"

Operation descriptions

### Columns

|Column|Type|PK|K2|Notes|
|---|---|---|---|---|
|eid|Integer||*|Event ID|
|ever|Integer||*|Event version|
|task|Integer||*|Task ID|
|opid|Integer||*|operation ID|
|prvid|Integer||*|Provider ID (ref to ProviderInfo)|
|opdesc|string|||operation description|

----

## Table "EventXml"

The raw event XML and record ID and nothing more. The key is to have
any interpretation and indexing in separate tables that
just refer to this table.

### Columns

|Column|Type|PK|Notes|
|---|---|---|---|
|rid|Integer|*|Record ID|
|xml|String||The raw XML record|

Note that the XML may occasionally be invalid because of
the presence of control characters. For parsing make sure
to disable character checking.

----

## Table "EventHeader"

The standard event information excluding the XML.
Note that the fields implicitly reference info rows in each of
ProviderInfo, TaskInfo and OperationInfo.
Note that fields other than prvid are put here directly instead
of using a lookup. This enables adding indexes if desired.

### Columns

|Column|Type|PK|Notes|
|---|---|---|---|
|rid|Integer|*|Record ID, PK|
|stamp|Integer||Time stamp as ticks since Epoch|
|eid|Integer||Event ID|
|ever|Integer||Event version|
|task|Integer||Task ID|
|prvid|Integer||Provider ID (ref to ProviderInfo)|
|opid|Integer||operation ID|




