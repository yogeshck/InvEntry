CREATE VIEW [dbo].[REP_SALES_INVRCT_DB_VIEW] AS

SELECT invoice_nbr as "InvNbr",
InvHdr.inv_date as "InvDate",
max(amount_payable) as "InvAmount",
max(case when transaction_type='Advance Adj' THEN adjusted_amount else 0 END) as "AdvanceAmt",
max(case when transaction_type='Discount' THEN adjusted_amount else 0 END) as "DiscountAmt",
max(case when transaction_type='Refund' THEN adjusted_amount else 0 END) as "Refund",
max(case when transaction_type='Recurring Deposit' THEN adjusted_amount else 0 END) as "RD",
max(case when transaction_type='Cash' THEN adjusted_amount else 0 END) as "Cash",
max(case when transaction_type='GPAY' THEN adjusted_amount else 0 END ) as "Gpay",
max(case when transaction_type='Credit Card' THEN adjusted_amount else 0 END ) as "CreditCard" ,
max(case when transaction_type='Debit Card' THEN adjusted_amount else 0 END) as "DebitCard",
max(case when transaction_type='Bank' THEN adjusted_amount else 0 END) as "Bank",
max(case when transaction_type='Credit' THEN adjusted_amount else 0 END) as "Credit",
max(case when transaction_type='Wire Transfer' THEN adjusted_amount else 0 END) as "Wire Transfer",
max(case when transaction_type='Bank Scanner' THEN adjusted_amount else 0 END) as "Bank Scanner",
max(case when transaction_type='mSwipe' THEN adjusted_amount else 0 END) as "mSwipe"
from dbo.INVOICE_AR_RECEIPTS as InvAR,
     dbo.INVOICE_HEADER as InvHdr
where InvHdr.gkey = InvAr.invoice_gkey
group by invoice_nbr, InvHdr.inv_date
