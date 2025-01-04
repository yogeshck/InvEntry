create view [dbo].[daily_stock_summary] AS
WITH FirstTransaction AS (
    -- This CTE gets the first transaction for each day
    SELECT
        pt.PRODUCT_CATEGORY,
        pt.opening_gross_weight,
        pt.transaction_gross_weight,
        pt.transaction_date,
        ROW_NUMBER() OVER (PARTITION BY pt.PRODUCT_CATEGORY, CAST(pt.transaction_date AS DATE) ORDER BY pt.transaction_date) AS rn
    FROM
        Product_Transaction pt
),
SumTransacted AS (
    -- This CTE calculates the sum of transacted quantities for each product per day
    SELECT
        pt.PRODUCT_CATEGORY,
        CAST(pt.transaction_date AS DATE) AS transacted_day,
SUM( IIF(pt.TRANSACTION_TYPE = 'Receipt', pt.transaction_gross_weight, 0)) AS 'StockIn',
SUM( IIF(pt.TRANSACTION_TYPE = 'Issue', pt.transaction_gross_weight, 0)) AS 'StockOut',
        SUM(pt.transaction_gross_weight) AS total_transacted_qty
    FROM
        Product_Transaction pt
    GROUP BY
        pt.PRODUCT_CATEGORY, CAST(pt.transaction_date AS DATE)
)
-- Now, combine the first transaction and sum of transacted quantities
SELECT ft.TRANSACTION_DATE,
    ft.PRODUCT_CATEGORY,
    ft.opening_gross_weight AS opening_qty_first_transaction,
st.StockIn,
st.StockOut,
    st.total_transacted_qty
FROM
    FirstTransaction ft
JOIN
    SumTransacted st
    ON ft.PRODUCT_CATEGORY = st.PRODUCT_CATEGORY
    AND CAST(ft.transaction_date AS DATE) = st.transacted_day
WHERE
    ft.rn = 1;  -- Only the first transaction of the day