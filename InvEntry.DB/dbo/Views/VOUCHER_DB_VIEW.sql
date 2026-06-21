CREATE VIEW [VOUCHER_DB_VIEW] AS
 SELECT [GKEY]    ,
[seq_nbr]
,[customer_gkey]
,[trans_type]
,[voucher_type]
,[mode]
,[trans_amount]
,[voucher_nbr]
,[voucher_date]
,[ref_doc_gkey]
,[ref_doc_nbr]
,[ref_doc_date]
,[trans_desc]
,[trans_date]
,CASE WHEN trans_type = 'Receipt' THEN trans_amount ELSE 0 END AS "Recd Amount"
,CASE WHEN trans_type = 'Payment' THEN trans_amount ELSE 0 END AS "Paid Amount"
,[from_ledger_gkey]
,FROM_LEDGER_NAME = (SELECT LEDGER_NAME FROM MTBL_LEDGERS MLF WHERE MLF.GKEY = VR.FROM_LEDGER_GKEY)
,[to_ledger_gkey]
,TO_LEDGER_NAME = (SELECT LEDGER_NAME FROM MTBL_LEDGERS MLT WHERE MLT.GKEY = VR.TO_LEDGER_GKEY)
,[ob_amount]
,[cb_amount]
,[fund_transfer_mode]
,[fund_transfer_ref_gkey]
,[fund_transfer_date]
  FROM [VOUCHER] VR
