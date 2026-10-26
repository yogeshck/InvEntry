# Schema gap decisions for mijms

`InvEntry.DB` is the source of truth for the **mijms** database. Its manifest is `..\schemagap.json`
(name, project, dacpac, options). Servers are not defined here: environments (dev, prod) are defined once
in the workspace, `WPF\deployment\schemagap.workspace.json`, which also lists the FinanceTracker database.

| Path | Purpose | Committed |
|---|---|---|
| `..\schemagap.json` | The mijms manifest. | Yes |
| `decisions\mijms.<environment>.json` | Go / no-go decisions with reasons, written by the Studio's **Save decisions**. | Yes |

Reports go to `WPF\deployment\schemagap-reports\` (not committed).

The tool is generic and lives in `D:\Repo_Git\SchemaGapStudio` (desktop app and `schemagap` command line).
How a release goes from here to production is drawn in `D:\Repo_Git\SchemaGapStudio\docs\release-flow.html`
(also **Release flow (F1)** in the Studio).
A schema change is made in this project's schema folders, never as a new migration script; see
`..\Scripts\README.md`.
