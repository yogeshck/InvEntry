SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

UPDATE dbo.MTBL_REFERENCES
SET IS_ACTIVE = 0
WHERE REF_NAME = 'STOCK_ADJUSTMENTS'
  AND (
      UPPER(LTRIM(RTRIM(REF_CODE))) = 'TRANSFER'
      OR UPPER(LTRIM(RTRIM(REF_VALUE))) = 'TRANSFER'
  );

DECLARE @Reasons TABLE
(
    REF_CODE VARCHAR(50) NOT NULL,
    REF_VALUE VARCHAR(50) NOT NULL,
    SORT_SEQ INT NOT NULL,
    MODULE VARCHAR(50) NOT NULL
);

INSERT INTO @Reasons (REF_CODE, REF_VALUE, SORT_SEQ, MODULE)
VALUES
    ('PHY_EXCESS',             'Physical Stock Excess',             10, 'INCREASE'),
    ('MISSED_RECEIPT',         'Missed Receipt',                     20, 'INCREASE'),
    ('WRONG_OUT_CORRECTION',   'Incorrect Stock-Out Correction',    30, 'INCREASE'),
    ('WEIGHT_CORRECTION_IN',   'Weight Correction - Increase',      40, 'INCREASE'),
    ('TAG_RECOVERY',           'Tag / Item Recovered',               50, 'INCREASE'),
    ('OPENING_CORRECTION_IN',  'Opening Stock Correction',          60, 'INCREASE'),
    ('PHY_SHORTAGE',           'Physical Stock Shortage',            10, 'DECREASE'),
    ('LOSS',                   'Loss',                               20, 'DECREASE'),
    ('DAMAGE',                 'Damage',                             30, 'DECREASE'),
    ('WRONG_IN_CORRECTION',    'Incorrect Stock-In Correction',     40, 'DECREASE'),
    ('WEIGHT_CORRECTION_OUT',  'Weight Correction - Decrease',      50, 'DECREASE'),
    ('TAG_MISSING',            'Tag / Item Missing',                 60, 'DECREASE'),
    ('OPENING_CORRECTION_OUT', 'Opening Stock Correction',          70, 'DECREASE'),
    ('COMPONENT_TRANSFER',     'Component Transfer',                 10, 'REALLOCATION'),
    ('HOOK_TRANSFER',          'Hook / Finding Transfer',            20, 'REALLOCATION'),
    ('STONE_TRANSFER',         'Stone Transfer',                     30, 'REALLOCATION'),
    ('DESIGN_MODIFICATION',    'Design Modification',                40, 'REALLOCATION'),
    ('CUSTOMER_MODIFICATION',  'Customer Modification',              50, 'REALLOCATION'),
    ('REPAIR_REALLOCATION',    'Repair Reallocation',                60, 'REALLOCATION'),
    ('OTHER',                  'Other',                               90, 'ALL');

UPDATE target
SET target.REF_VALUE = source.REF_VALUE,
    target.SORT_SEQ = source.SORT_SEQ,
    target.MODULE = source.MODULE,
    target.IS_ACTIVE = 1
FROM dbo.MTBL_REFERENCES AS target
INNER JOIN @Reasons AS source
    ON source.REF_CODE = target.REF_CODE
WHERE target.REF_NAME = 'STOCK_ADJUSTMENTS';

INSERT INTO dbo.MTBL_REFERENCES
    (REF_NAME, REF_CODE, REF_VALUE, SORT_SEQ, REF_DESC, MODULE, IS_ACTIVE)
SELECT
    'STOCK_ADJUSTMENTS',
    source.REF_CODE,
    source.REF_VALUE,
    source.SORT_SEQ,
    NULL,
    source.MODULE,
    1
FROM @Reasons AS source
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.MTBL_REFERENCES AS target
    WHERE target.REF_NAME = 'STOCK_ADJUSTMENTS'
      AND target.REF_CODE = source.REF_CODE
);

COMMIT TRANSACTION;
