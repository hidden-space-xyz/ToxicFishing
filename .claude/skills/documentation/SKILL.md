---
name: documentation
description: >-
  Documentation standards for ToxicFishing. Use when adding or changing any non-private type or
  member (which requires XML doc comments), or when a change alters behaviour, features, build
  steps, or usage (which may require a README update). Ensures the public surface is documented and
  user-facing docs stay accurate.
---

# Documentation

Two obligations, both part of the Definition of Done.

## 1. XML doc comments on every non-`private` member

Any type or member that is **not `private`** (`public`, `internal`, `protected`) must carry XML doc
comments. `private` members are documented only when the logic is non-obvious (use a short `//`
comment there instead).

Whenever you add or touch a non-`private` member, give it proper XML docs. Don't leave a member you
just edited undocumented.

House style:

- `<summary>` explains *what and why*, not just a restatement of the name.
- Document **every** parameter with `<param>`, the return with `<returns>`, and each thrown exception
  callers should handle with `<exception>`.
- Use `<see cref="..."/>` / `<paramref name="..."/>` for cross-references, and
  `<see langword="true"/>`/`false`/`null` for keywords.
- Document **enum members** individually (e.g. `ActivityLevel`).
- Keep it accurate: if you change a signature or behaviour, update its doc comment in the same edit.
  Roslynator flags malformed doc tags as warnings — keep the build clean.

Example shape:

```csharp
/// <summary>
/// Scans the captured frame for the bobber and reports its screen position, biasing toward the last
/// known location so a freshly-cast float is preferred over background noise.
/// </summary>
/// <param name="screenPoint">The detected bobber position in screen pixels, or empty if none found.</param>
/// <returns><see langword="true"/> if a bobber was located; otherwise <see langword="false"/>.</returns>
```

## 2. Keep `README.md` accurate

Update `README.md` when a change affects anything a reader relies on:

- A new or changed **feature**, detection behaviour, or supported game/process → update the
  *What it does* / *Why* sections.
- A change to **build, run, or prerequisite** steps (it is **Windows-only**, .NET 10) → update
  *How to Use* / build notes.
- A change to the **project structure** or a project's responsibility → update the structure notes.
- A change to the **tuning model** (`AppOptions`) or the runtime bobber-swap → update the detection
  tuning note.
- Any change to **responsible-use / Terms-of-Service** behaviour → keep the warning prominent.

Do **not** touch the README for purely internal refactors a user or contributor would never observe.
When unsure whether a change is user-visible, it probably belongs in the README.

## Don't over-document

- No redundant comments that just echo the code.
- No commented-out code, changelog noise, or "added by" attributions in source.
- Prefer clearer names and types over explanatory comments where possible.
