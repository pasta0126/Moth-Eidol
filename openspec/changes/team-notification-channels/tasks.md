## 1. Core

- [ ] 1.1 Team channel settings model, validation and migration
- [ ] 1.2 Secret storage abstraction with Windows and macOS implementations
- [ ] 1.3 Bounded delivery queue with timeout, retry/backoff and failure recording
- [ ] 1.4 Webhook channel with generic payload and Slack/Teams presets
- [ ] 1.5 Email channel over SMTP/TLS (after the dependency decision) or defer
- [ ] 1.6 Register channels in the chain honoring minimum level and the frequency policy

## 2. UI

- [ ] 2.1 Team channels section in project settings
- [ ] 2.2 Send-test action and last delivery status
- [ ] 2.3 Local-only/duplicates note

## 3. Validation

- [ ] 3.1 Tests with a fake HTTP server: success, timeout, retry, drop, minimum level
- [ ] 3.2 Verify secrets never appear in logs or exports
- [ ] 3.3 Update specs and CHANGELOG at release
