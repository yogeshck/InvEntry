/*
    Post-deployment script. Runs after the schema changes on every deployment.
    Everything here must be safe to run repeatedly.

    Include reference data with SQLCMD syntax, relative to this file.
    StockAdjustmentReasons.sql is idempotent but updates existing rows (deactivates TRANSFER, renames
    reasons), so it is not enabled yet. Uncomment once production values have been checked.
*/
-- :r ..\ReferenceData\StockAdjustmentReasons.sql
