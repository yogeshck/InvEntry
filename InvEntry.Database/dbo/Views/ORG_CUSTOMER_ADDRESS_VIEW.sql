
create view [dbo].[ORG_CUSTOMER_ADDRESS_VIEW] as
SELECT 
		orgCustomer.CUSTOMER_NAME			AS "CUST_NAME", 
		orgCustomer.PAN_NBR 				AS "PAN_NBR", 
		orgCustomer.CREDIT_AVAILED			AS "CREDIT_AVAILED", 
		orgCustomer.STATUS 					AS "CUST_STATUS", 
		orgCustomer.CUSTOMER_SINCE			AS "CUST_SINCE", 
		orgCustomer.SALUTATIONS				AS "SALUTATIONS", 
        orgCustomer.CUSTOMER_TYPE 			AS "CUST_TYPE", 
		orgAddress.ADDRESS_LINE1 			AS "ADD_LINE1", 
		orgAddress.ADDRESS_LINE2 			AS "ADD_LINE2",
		orgAddress.ADDRESS_LINE3 			AS "ADD_LINE3",
		orgAddress.AREA						AS "AREA",
		orgAddress.CITY						AS "CITY", 
		orgAddress.GST_STATE_CODE 			AS "CUST_GST_CODE", 
		orgAddress.DISTRICT					AS "DISTRICT", 
		orgAddress.STATE					AS "STATE", 
		orgAddress.COUNTRY					AS "COUNTRY", 
		orgCustomer.MOBILE_NBR				AS "MOBILE_NBR", 
		orgCustomer.GKEY					AS "CUST_GKEY", 
		orgCustomer.CLIENT_ID				AS "CLIENT_ID", 
		orgCustomer.GSTIN_NBR				AS "GSTIN_NBR", 
		orgCustomer.LOCATION_GKEY			AS "LOCATION_GKEY", 
		orgCustomer.ADDRESS_GKEY			AS "ADDRESS_GKEY"
FROM    dbo.ORG_CUSTOMER AS orgCustomer INNER JOIN
        dbo.ORG_ADDRESS AS orgAddress ON orgCustomer.ADDRESS_GKEY = orgAddress.GKEY