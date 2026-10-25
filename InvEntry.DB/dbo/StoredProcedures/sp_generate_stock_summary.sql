CREATE PROCEDURE [dbo].[sp_generate_stock_summary] (@report_date DATE)
AS
BEGIN
    SET NOCOUNT ON;

    -- Insert or update summary per product
-- Insert or update summary per product
    MERGE DAILY_REP_STOCK_SUMRY_MOVEMENT AS target
    USING (

    SELECT
        dm.product,
        dm.branch_id AS branch_id,
        dm.metal AS metal,
        ISNULL(prev.closing_qty,0) AS opening_qty,
        ISNULL(prev.closing_weight,0) AS opening_weight,
        SUM(dm.in_qty) AS in_qty,
        SUM(dm.in_gross_weight) AS in_weight,
        SUM(dm.out_qty) AS out_qty,
        SUM(dm.out_gross_weight) AS out_weight
    FROM DAILY_REP_STOCK_MOVEMENT dm
    LEFT JOIN DAILY_REP_STOCK_SUMRY_MOVEMENT prev
        ON prev.product = dm.product
       AND prev.report_date = DATEADD(DAY,-1,@report_date)
    WHERE dm.report_date = @report_date
    GROUP BY dm.product, dm.metal, dm.branch_id, prev.closing_qty, prev.closing_weight
) AS src

    ON target.report_date = @report_date AND target.product = src.product
    WHEN MATCHED THEN
        UPDATE SET
            opening_qty = src.opening_qty,
            opening_weight = src.opening_weight,
            in_qty = src.in_qty,
            in_gross_weight = src.in_weight,
            out_qty = src.out_qty,
            out_gross_weight = src.out_weight,
            closing_qty = src.opening_qty + src.in_qty - src.out_qty,
            closing_weight = src.opening_weight + src.in_weight - src.out_weight
    WHEN NOT MATCHED THEN
        INSERT (report_date, branch_id, product, metal, opening_qty, opening_weight,
                in_qty, in_gross_weight, out_qty, out_gross_weight, closing_qty, closing_weight)
        VALUES (@report_date, src.branch_id, src.product, src.metal, src.opening_qty, src.opening_weight,
                src.in_qty, src.in_weight, src.out_qty, src.out_weight,
                src.opening_qty + src.in_qty - src.out_qty,
                src.opening_weight + src.in_weight - src.out_weight);


    -- Products with no movement today: carry forward yesterday’s closing
        INSERT DAILY_REP_STOCK_SUMRY_MOVEMENT (report_date, branch_id, product, metal, opening_qty, opening_weight,
                in_qty, in_gross_weight, out_qty, out_gross_weight, closing_qty, closing_weight)
    SELECT
        @report_date,
        prev.branch_id AS branch_id,
        prev.product,
        prev.metal AS metal,
        prev.closing_qty AS opening_qty,
        prev.closing_weight AS opening_weight,
        0 AS in_qty,
        0 AS in_weight,
        0 AS out_qty,
        0 AS out_weight,
        prev.closing_qty AS closing_qty,       -- carry forward yesterday’s closing
        prev.closing_weight AS closing_weight
    FROM DAILY_REP_STOCK_SUMRY_MOVEMENT prev
    WHERE prev.report_date = DATEADD(DAY,-1, @report_date)
      AND NOT EXISTS (
          SELECT dm.product, dm.REPORT_DATE, dm.TYPE
          FROM dbo.DAILY_REP_STOCK_MOVEMENT dm
          WHERE dm.report_date = @report_date
            AND dm.product = prev.product
      )
END

GO
