## Context

`notification-channels` exposes a pluggable chain; today all channels are local (toast, tray, sound, native Windows notification). Alert production and the frequency policy (cooldown/summary) already exist per project.

## Decisions

- Team channels are implementations of the existing channel abstraction, configured per project; they receive the already-throttled deliveries, so no second throttling.
- Remote delivery runs off the alert path through a small bounded in-memory queue with timeout, bounded retries (e.g. 3 with backoff) and drop-on-overflow; local channels are never awaited behind it.
- Webhook first: `HttpClient`, JSON payload with a stable schema and Slack/Teams-compatible presets. Email second, as SMTP over TLS.
- Secrets: webhook URLs and SMTP credentials stored per user via OS credential storage (Windows Credential Manager / macOS Keychain) behind a platform abstraction; fall back to the local database only with an explicit warning if unavailable (to decide at implementation).
- Dependencies: webhook needs none. For email, `System.Net.Mail.SmtpClient` is discouraged by Microsoft; adding MailKit must be justified here before implementation, or email is deferred and shipped webhook-only. Decision pending with the maintainer.
- No deduplication across teammates (there is no shared state); documented in the UI.
- Exports (`project-config-export-import`) must exclude all team channel secrets.

## Risks

- Duplicate notifications when several teammates monitor the same project.
- Leaked webhook URLs equal write access to a channel; hence protected storage and exclusion from exports.
- Corporate proxies and TLS inspection can break delivery; surface the real error in the test action.
- Scope growth (retries, templates, more services); keep the payload fixed and generic in this change.
