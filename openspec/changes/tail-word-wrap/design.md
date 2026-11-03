## Decisions

- The preference is a single application-wide setting stored with the existing settings storage, default on. Rationale: it is a viewing preference, not a project parameter. Assumption recorded: not per project.
- Wrapping is done by toggling the text wrapping of the line text and the horizontal scroll visibility; the timestamp and level columns keep their width and the message column takes the remaining width, so wrapped lines align under the message.
- Auto-scroll must account for variable row heights so the newest line stays visible.

- The event detail has no toolbar of its own; it reads the same application-wide setting (changed from the Tail checkbox) and, to keep it discoverable, shows the same "Word wrap" checkbox bound to it in the detail header. Assumption recorded.

## Risks

- Variable row heights with a large number of lines: verify scrolling performance with the maximum lines setting and the log simulator.
