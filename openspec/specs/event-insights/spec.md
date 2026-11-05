# event-insights Specification

## Purpose
Defines the human-oriented presentation and analysis of event groups: readable titles, relative times, trends, badges, an ordered stack trace, the context before an error and a shareable summary.

## Requirements

### Requirement: Readable title and location
The system SHALL show for each event group a title made of the exception type and message when the occurrence contains an exception, and the first line of the entry message otherwise, and SHALL show as its location the most relevant application frame.

#### Scenario: Entry with an exception
- **WHEN** an error entry is followed by `System.InvalidOperationException: boom` and frames
- **THEN** the group's title is `InvalidOperationException: boom` and its location is the first application frame, for example `Worker.ExecuteAsync`

#### Scenario: Entry without an exception
- **WHEN** an error entry has no stack trace
- **THEN** the title is the entry's first line and no location is shown

#### Scenario: Only framework frames
- **WHEN** an exception has only framework frames
- **THEN** no application location is shown

### Requirement: Relative times
The system SHALL show when a group was last and first seen as relative times such as "just now", "3 min ago", "2 h ago" and "yesterday", with the exact time available on demand.

#### Scenario: Recent event
- **WHEN** a group's last occurrence was three minutes ago
- **THEN** its row shows "3 min ago"

#### Scenario: Old event
- **WHEN** a group's last occurrence was more than two days ago
- **THEN** its row shows the date

### Requirement: Frequency sparkline
The system SHALL show on each group row and on each dashboard tile a small chart of the number of events per hour over the last 24 hours.

#### Scenario: Burst of errors
- **WHEN** a group had many occurrences in the last hours
- **THEN** its sparkline shows the hourly counts with the bursts visible

#### Scenario: Project tile
- **WHEN** a project has alert-level events
- **THEN** its tile shows the sparkline of all its alert-level events

### Requirement: New and regressed badges
The system SHALL mark a group as New when it was first seen in the last 24 hours and as Regressed when it received occurrences after being marked as seen, and SHALL clear Regressed when it is marked as seen again.

#### Scenario: New error
- **WHEN** an error appears for the first time
- **THEN** its group is badged New for the next 24 hours

#### Scenario: Error returns
- **WHEN** an occurrence arrives for a group previously marked as seen
- **THEN** the group is badged Regressed until it is marked as seen again

### Requirement: Tile health summary
The system SHALL show on each tile the number of alert-level events in the last 24 hours and whether that is more, fewer or the same as in the 24 hours before.

#### Scenario: Rising errors
- **WHEN** a project had more events in the last 24 hours than in the 24 hours before
- **THEN** its tile shows the count and a rising trend

#### Scenario: Quiet project
- **WHEN** a project had no events in either period
- **THEN** its tile shows zero and no trend

### Requirement: Ordered stack trace
The system SHALL show the stack trace of an occurrence as an ordered list of frames with file and line, with application frames highlighted and framework frames collapsed until expanded, with inner exceptions shown separately and unrecognized text kept as plain text.

#### Scenario: Mixed frames
- **WHEN** a stack trace has application and framework frames
- **THEN** the application frames are highlighted and the framework frames are collapsed behind an expander

#### Scenario: Inner exception
- **WHEN** a stack trace contains an inner exception
- **THEN** it is shown as a separate block under the outer exception

### Requirement: Context before the error
The system SHALL save with each stored occurrence the entries that immediately preceded it from the same source, up to a configurable number, and SHALL show them in the detail with their level and relative time.

#### Scenario: Error after other activity
- **WHEN** an error is logged after several information entries from the same source
- **THEN** its detail shows those preceding entries in order

#### Scenario: First entry of a source
- **WHEN** an error is the first entry read from a source
- **THEN** its detail shows no context

### Requirement: Occurrence timeline
The system SHALL show in the group detail a timeline of its occurrences over a selectable range of the last 24 hours, by hour, or the last 7 days, by day.

#### Scenario: Change range
- **WHEN** the user switches from 24 hours to 7 days
- **THEN** the chart shows one bar per day for the last seven days

### Requirement: Copy summary
The system SHALL let the user copy to the clipboard a text summary of a group with its project, title, location, level, counts, first and last seen, pending state and the latest occurrence's stack trace.

#### Scenario: Copy
- **WHEN** the user chooses copy summary on a group
- **THEN** the clipboard contains the summary text ready to paste in a chat or ticket

### Requirement: Persistence of time series
The system SHALL persist the hourly counts of each group for at least seven days across restarts, applying the retention of its group.

#### Scenario: Restart
- **WHEN** the application restarts
- **THEN** sparklines, trends and timelines still show the previous hours

### Requirement: Copy from the event detail
The system SHALL let the user select text within the context entries and the occurrence text of an event detail, SHALL let the user copy the selected text with the standard shortcut and a context menu, and SHALL offer copying all context entries plus the occurrence text.

#### Scenario: Copy part of a context entry
- **WHEN** the user selects text in a context entry and copies it
- **THEN** the clipboard contains the selected text

#### Scenario: Copy context and occurrence
- **WHEN** the user chooses "Copy all" in the event detail
- **THEN** the clipboard contains the context entries followed by the occurrence text in display order

#### Scenario: Selection with wrapping
- **WHEN** the lines are wrapped and the user selects across wrapped lines
- **THEN** the copied text contains the original line breaks only, not the visual wraps

### Requirement: Word wrap in event detail
The system SHALL wrap the context entries and the occurrence text of an event detail, including its stack trace, to the available width according to the same word wrap option as the tail, enabled by default, and SHALL offer that option in a view actions bar at the top-right corner of the event detail.

#### Scenario: Default wrapping
- **WHEN** the user opens an event whose context or stack trace has long lines
- **THEN** the lines wrap within the detail without horizontal scrolling

#### Scenario: Disable from the detail
- **WHEN** the user clears the word wrap checkbox in the event detail
- **THEN** the context and occurrence lines show on one line each with horizontal scrolling, and the tail uses the same choice

#### Scenario: Shared choice
- **WHEN** the user disables word wrap in the tail and then opens an event
- **THEN** the event detail also has word wrap disabled

#### Scenario: Placement
- **WHEN** the user opens an event
- **THEN** the Word wrap checkbox is at the top-right corner of the detail, above its title
