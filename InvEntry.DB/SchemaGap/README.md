# Schema gap config for mijms

This folder holds only what is specific to the mijms database. The tool itself is generic and lives in its
own repository, `D:\Repo_Git\SchemaGapStudio` (desktop app and command line).

| File | Purpose | Committed |
|---|---|---|
| `databases.json` | Where the schema source is (`..\InvEntry.DB.sqlproj`), the comparison rules, and the targets (dev, prod). | Yes |
| `decisions\mijms.<target>.json` | Saved go / no-go decisions with reasons, written by the Studio's **Save decisions**. | Yes |
| `reports\` | Reports and generated scripts per run. | No |

## Use

- Desktop: run Schema Gap Studio, **Open…** this `databases.json`, choose mijms and a target, **Compare**.
- Command line:

```powershell
D:\Repo_Git\SchemaGapStudio\cli\schema-gap.ps1 report -Database mijms -Target dev -Config .\databases.json
```

Before comparing against production, set `targets.prod.server` (and `backupDir`, a folder on that server).

The source of truth is the schema in `InvEntry.DB` (`dbo\`, `hr\`, `Security\`). A schema change is made
there, never as a new migration script; see `..\Scripts\README.md`.
