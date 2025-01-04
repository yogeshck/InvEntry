
CREATE VIEW [dbo].[ORG_THIS_COMPANY_VIEW] AS
	SELECT
    orgCompany.name AS company_name,
    orgAddress.address_line1 AS address_line1,
    orgAddress.address_line2 AS address_line2,
    orgAddress.area AS area,
    orgAddress.state AS state,
    orgAddress.gst_state_code AS gst_code,
    orgAddress.country,
    orgAddress.district,
    orgAddress.city,	
    orgCompany.pan_nbr,
	orgCompany.gst_nbr,
	orgCompany.this_company
FROM
    org_company       orgCompany,
    org_address       orgAddress
WHERE
    orgCompany.address_gkey = orgAddress.gkey