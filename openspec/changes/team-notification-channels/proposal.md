## Why

Alerts only reach the person whose PC is running the app. Teams want a shared place (a chat channel, a mailbox, an incident tool) to be told when a project has a problem, without everyone relying on their own laptop being on.

## What Changes

- Add team-wide notification channels on top of the pluggable channel chain: **webhook** (generic JSON POST, with ready presets for Slack/Teams-style incoming webhooks) and **email** (SMTP).
- Channels are configured per project (destination, enabled, minimum level) with a "send test" action; credentials stay local to the user's machine.
- They obey the project's frequency policy (cooldown or summary) and never block or slow the local channels.
- Failures are recorded and surfaced without losing the alert; delivery is best-effort with a small bounded retry.
- Still only while the app is running: this is not a server, and each user decides to enable it. Duplicate sends by several teammates monitoring the same project are possible and documented.

## Capabilities

### New Capabilities
- `team-channels`: webhook and email delivery of alerts.

### Modified Capabilities
- `notification-channels`: registers the team channels in the chain.
- `projects`: project configuration includes team channel settings.
- `project-screen`: settings include a team channels section.

## Impact

- Core: channel implementations behind the existing abstraction, delivery queue with retry, persistence migration, secret storage.
- App: project settings section, test-send, delivery failure indicator.
- Dependencies: webhook uses `HttpClient` (none new). Email likely needs an SMTP client (MailKit) to be justified in design before adding; alternative is webhook-only first.
