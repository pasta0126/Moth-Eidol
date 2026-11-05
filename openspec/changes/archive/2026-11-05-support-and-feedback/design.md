## Context

Application settings (autostart, notification channels) live in `AppSettingsView`, which already uses `Moth*Brush` resources and selectable text. The app never opens web pages today and the privacy policy promises that it does not connect to any server. Feedback must therefore go through a public, visible place chosen by the user, not through a private database or a form that could look like data collection.

## Goals / Non-Goals

**Goals:**
- A discreet, warm About block that does not draw attention.
- Public feedback through GitHub Issues with structured templates.
- Keep the "no data leaves the app" promise true and documented.

**Non-Goals:**
- In-app forms, telemetry, crash reporting or automatic attachment of logs or settings.
- Detecting or storing whether the user donated.
- Native "share" or e-mail based feedback.

## Decisions

- **Placement:** only at the end of the application settings screen. Rationale: it must not call attention.
- **Presentation:** small secondary text (`MothTextMutedBrush`) and plain text links (accent color), not buttons.
- **Text:** first person, signed `pasta0126`, no technologies, one emoji. Proposed text: "I'm pasta0126, a developer from Barcelona who loves turning ideas into small tools you can click, explore and hopefully enjoy. If Moth-Eidol makes your day a little easier, you can buy me a coffee ☕"
- **Links:** Buy me a coffee `https://buymeacoffee.com/pasta0126`; Suggestions & bugs `https://github.com/pasta0126/Moth-Eidol/issues/new/choose`; GitHub `https://github.com/pasta0126/`.
- **Opening links:** an abstraction in the app platform layer (same style as the other OS-specific code) that opens a URL in the default browser. The URLs are constants of the app, never built from user data, and only `https` URLs are opened.
- **Feedback channel:** GitHub Issues with two templates (suggestion, bug report). The bug report asks for app version and OS and warns not to paste logs with sensitive data. `config.yml` disables blank issues and adds a contact link to Buy me a coffee. Templates are in English; users may write in any language.
- **Privacy policy:** add that the links only open the browser, the app sends no data on its own, and feedback is whatever the user writes on GitHub.

## Risks / Trade-offs

- A GitHub account is needed to leave feedback; acceptable for a technical audience.
- Microsoft Store review may look at donation links; keeping it a discreet link in settings, with no feature locked behind it, is the safest form.
- Opening the default browser can fail on a locked-down machine; the failure must not crash the app (show a short message or copy the URL instead).
