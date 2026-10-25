# InvEntry.DB scripts

The schema itself (tables, indexes, views, procedures) lives in the schema folders (`dbo\`, `hr\`, `Security\`)
and is the source of truth. Deployments compare that schema with the target database
(`SchemaGap\schema-gap.ps1`) and generate the change script. The folders here hold everything that a schema
comparison cannot express.

| Folder | What goes here | Runs |
|---|---|---|
| `PreDeployment\` | `Script.PreDeployment.sql`: steps that must happen before schema changes (e.g. clean data so a new constraint can be added). | Automatically, before every deployment. Must be safe to re-run. |
| `PostDeployment\` | `Script.PostDeployment.sql`: includes reference data with `:r`. | Automatically, after every deployment. Must be safe to re-run. |
| `ReferenceData\` | Idempotent lookup/master data (`MERGE` / insert-if-missing). | Through `PostDeployment`, or by hand. |
| `Upgrades\<version>\` | Data changes that belong to one release, e.g. `Upgrades\v1.0.141\01_backfill_status.sql`. | Once per database, in file order, after the schema deployment for that release. |
| `Patches\` | One-off hotfixes applied outside a release, named `yyyy-MM-dd_description.sql`. | Once, by hand, then recorded in the release notes. |
| `Migrations\` | Historical hand-written migrations (2026-10) from before the schema project was the source of truth. | Reference only. New schema changes go in the schema folders instead. |
| `Deploy\` | Environment setup: logins, users, permissions. | By hand per environment (not compared by the schema-gap tool). |

## Rules

- A schema change (new table, column, index, view, procedure) is made in the schema folders, never as a new
  migration script.
- Every script in `PreDeployment`, `PostDeployment` and `ReferenceData` must be safe to run repeatedly.
- `Upgrades` and `Patches` scripts start with a header saying what they do, whether they change data, and
  whether they can be re-run.
