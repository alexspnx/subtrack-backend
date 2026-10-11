---
name: git-commit
description: Create a git commit in this repository using the team's commit message format (`~feat: message [ST-123]`, with `!` for breaking changes). Use this whenever you are asked to commit changes, write a commit message, or check that a commit message follows the convention. Takes an optional ticket number as its argument.
---

# git-commit

Every commit in this repository uses one subject-line format, so the history shows at a glance whether a
change is safe and which ticket it belongs to.

## Commit message format

```
<impact><scope>: <message> [<TICKET>]
```

Example: `~feat: add subscription endpoints [ST-004]`

| Part | Rule |
|---|---|
| `<impact>` | `~` for a safe commit, `!` for a breaking one. See "Safe or breaking" below. |
| `<scope>` | Exactly one of `feat`, `fix`, `chore`, `refactor`, `test`. See "Choosing the scope" below. |
| `: ` | A colon and one space, directly after the scope. |
| `<message>` | A short summary of what the commit does: imperative mood, lowercase first letter, no trailing period (`send current statistics to connected clients`, not `Sent statistics.`). |
| `[<TICKET>]` | The ticket number in square brackets, as the last thing on the line. |

No space goes between the impact character and the scope: `~feat:`, never `~ feat:`.

If the change needs more explanation, leave a blank line after the subject and write a body. The subject line
itself always follows the format above.

### Choosing the scope

- `feat`: a new feature or new behavior.
- `fix`: a bug fix.
- `chore`: maintenance that doesn't change behavior, such as dependency bumps, build/config changes, docs, or
  tooling.
- `refactor`: restructuring code without changing its behavior.
- `test`: adding or changing tests only.

If a commit mixes kinds, use the scope of the main change (a feature with its tests is `feat`). **If no scope
clearly fits, use `chore`.** That is the fallback.

### Safe or breaking

Use `!` when the commit can break something outside the commit itself: another deployable, a client, or a deployment. In this solution that includes:

- any API change that the "Versioning and compatibility" section of `docs/api-contract.md` calls breaking:
  for example removing or renaming an endpoint, or a request/response field in `ViewModels`; changing a
  field's type or format; changing a route, status code or error `code`; tightening validation; or changing a
  calculation result or an expectation in `docs/recurrence-examples.json`;
- changing a message contract (`DTOs`) that other systems depend on;
- a database schema change (EF migration) that isn't backward compatible;
- renaming or removing configuration keys, environment variables, or deployment settings.

API changes that the contract doc lists as additive (a new endpoint, an optional request field, a new response
field, …) are `~`. Everything else is `~` too. If you can't tell whether a change is breaking, ask the user rather than guessing.

### The ticket number

Every commit needs a ticket number, because it is how commits are traced back to work items. Find it in this
order:

1. The argument passed to this skill, or a ticket the user named in the conversation.
2. The spec or story being worked on (e.g. `spec-st-004-…` → `ST-004`).

If neither gives one, **ask the user for it**. Never invent a ticket number or leave the brackets empty.

## Steps

1. **Review what will be committed.** Run `git status` and `git diff` (plus `git diff --staged` for anything
   already staged). Make sure nothing unintended is included, such as secrets, `.env` files, local settings,
   or build output. If the working tree holds unrelated changes, ask the user which ones belong in this
   commit.
2. **Check the branch.** Commit directly on `main`; this repo uses no feature branches or PRs. If you're on
   any other branch, ask the user before committing. Push only `main`, and only when the user asks.
3. **Verify the build.** If the commit touches `src/`, `tests/`, a `.csproj`, a `.props` file, or the
   `.slnx`, run `dotnet build subtrack-backend.slnx` and
   `dotnet test --solution subtrack-backend.slnx` first. Don't commit if either fails; report
   the failure instead.
4. **Stage** the intended files by path. Avoid `git add -A` unless you've confirmed every change belongs in
   this commit.
5. **Write the message** following the format above: pick the impact, pick the scope, summarize the change,
   and add the ticket.
6. **Commit** with `git commit -m "<subject>"` (add a body with a second `-m` if needed). Never use
   `--no-verify`, and never amend or force-push unless the user explicitly asks. If a hook fails, fix the
   cause and create the commit again.
7. **Report** the commit hash and the subject line to the user.

## Checking an existing message

When asked to validate a commit message, check it against this pattern:

```
^[~!](feat|fix|chore|refactor|test): \S.*\S \[[^\]\s]+\]$
```

Then check what a regex can't: that the scope and the `~`/`!` choice fit the actual change.
