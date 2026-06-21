
CREATE VIEW [dbo].[ORG_THIS_COMPANY_VIEW] AS
SELECT 	orgCompany.NAME AS company_name, 
		orgAddress.ADDRESS_LINE1, 
		orgAddress.ADDRESS_LINE2, 
		orgAddress.ADDRESS_LINE3,		
		orgAddress.AREA, 
		orgAddress.STATE, 
		orgAddress.PINCODE,		 
		orgAddress.COUNTRY, 
		orgAddress.DISTRICT, 
		orgAddress.CITY, 
		orgCompany.CONTACT_NBR1,
		orgCompany.CONTACT_NBR2,
		orgCompany.EMAIL_ID,
		orgCompany.PAN_NBR, 
		orgCompany.TENANT_GKEY,
		orgCompany.SERVICE_TAX_NBR,
		orgCompany.GST_NBR,
		orgAddress.GST_STATE_CODE AS gst_code,
		orgCompany.THIS_COMPANY,
		orgCompany.TAGLINE
FROM    dbo.ORG_COMPANY AS orgCompany 
INNER JOIN dbo.ORG_ADDRESS AS orgAddress ON orgCompany.ADDRESS_GKEY = orgAddress.GKEY
WHERE  (orgCompany.THIS_COMPANY = '1')
