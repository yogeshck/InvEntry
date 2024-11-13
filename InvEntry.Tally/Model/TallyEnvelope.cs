using InvEntry.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InvEntry.Tally;

[XmlRoot(ElementName = "HEADER")]
public class HEADER : IInvEntryXmlSerializable
{

    [XmlElement(ElementName = "TALLYREQUEST")]
    public string TALLYREQUEST { get; set; }
}

[XmlRoot(ElementName = "STATICVARIABLES")]
public class STATICVARIABLES : IInvEntryXmlSerializable
{

    [XmlElement(ElementName = "SVCURRENTCOMPANY")]
    public string SVCURRENTCOMPANY { get; set; }
}

[XmlRoot(ElementName = "REQUESTDESC")]
public class REQUESTDESC : IInvEntryXmlSerializable
{

    [XmlElement(ElementName = "REPORTNAME")]
    public string REPORTNAME { get; set; }

    [XmlElement(ElementName = "STATICVARIABLES")]
    public STATICVARIABLES STATICVARIABLES { get; set; }
}
    
[XmlRoot(ElementName = "OLDAUDITENTRYIDS.LIST")] 
public class OLDAUDITENTRYIDSLIST : IInvEntryXmlSerializable
{

    [XmlElement(ElementName = "OLDAUDITENTRYIDS")]
    public int OLDAUDITENTRYIDS { get; set; }

    [XmlAttribute(AttributeName = "TYPE")]
    public string TYPE { get; set; }

    [XmlText]
    public string Text { get; set; }
}

[XmlRoot(ElementName = "GSTREGISTRATION")]
public class GSTREGISTRATION : IInvEntryXmlSerializable
{

    [XmlAttribute(AttributeName = "TAXTYPE")]
    public string TAXTYPE { get; set; }

    [XmlAttribute(AttributeName = "TAXREGISTRATION")]
    public string TAXREGISTRATION { get; set; }

    [XmlText]
    public string Text { get; set; }
}

[XmlRoot(ElementName = "RATEDETAILS.LIST")]
public class RATEDETAILSLIST : IInvEntryXmlSerializable
{

    [XmlElement(ElementName = "GSTRATEDUTYHEAD")]
    public string GSTRATEDUTYHEAD { get; set; }
}

[XmlRoot(ElementName = "ALLLEDGERENTRIES.LIST")]
public class ALLLEDGERENTRIESLIST : IInvEntryXmlSerializable
{

    [XmlElement(ElementName = "OLDAUDITENTRYIDS.LIST")]
    public OLDAUDITENTRYIDSLIST OLDAUDITENTRYIDSLIST { get; set; }

    [XmlElement(ElementName = "LEDGERNAME")]
    public string LEDGERNAME { get; set; }

    [XmlElement(ElementName = "GSTCLASS")]
    public string GSTCLASS { get; set; }

    [XmlElement(ElementName = "GSTOVRDNISREVCHARGEAPPL")]
    public string GSTOVRDNISREVCHARGEAPPL { get; set; }

    [XmlElement(ElementName = "GSTOVRDNSTOREDNATURE")]
    public object GSTOVRDNSTOREDNATURE { get; set; }

    [XmlElement(ElementName = "GSTOVRDNTYPEOFSUPPLY")]
    public string GSTOVRDNTYPEOFSUPPLY { get; set; }

    [XmlElement(ElementName = "GSTRATEINFERAPPLICABILITY")]
    public string GSTRATEINFERAPPLICABILITY { get; set; }

    [XmlElement(ElementName = "GSTHSNINFERAPPLICABILITY")]
    public string GSTHSNINFERAPPLICABILITY { get; set; }

    [XmlElement(ElementName = "ISDEEMEDPOSITIVE")]
    public string ISDEEMEDPOSITIVE { get; set; }

    [XmlElement(ElementName = "LEDGERFROMITEM")]
    public string LEDGERFROMITEM { get; set; }

    [XmlElement(ElementName = "REMOVEZEROENTRIES")]
    public string REMOVEZEROENTRIES { get; set; }

    [XmlElement(ElementName = "ISPARTYLEDGER")]
    public string ISPARTYLEDGER { get; set; }

    [XmlElement(ElementName = "GSTOVERRIDDEN")]
    public string GSTOVERRIDDEN { get; set; }

    [XmlElement(ElementName = "ISGSTASSESSABLEVALUEOVERRIDDEN")]
    public string ISGSTASSESSABLEVALUEOVERRIDDEN { get; set; }

    [XmlElement(ElementName = "STRDISGSTAPPLICABLE")]
    public string STRDISGSTAPPLICABLE { get; set; }

    [XmlElement(ElementName = "STRDGSTISPARTYLEDGER")]
    public string STRDGSTISPARTYLEDGER { get; set; }

    [XmlElement(ElementName = "STRDGSTISDUTYLEDGER")]
    public string STRDGSTISDUTYLEDGER { get; set; }

    [XmlElement(ElementName = "CONTENTNEGISPOS")]
    public string CONTENTNEGISPOS { get; set; }

    [XmlElement(ElementName = "ISLASTDEEMEDPOSITIVE")]
    public string ISLASTDEEMEDPOSITIVE { get; set; }

    [XmlElement(ElementName = "ISCAPVATTAXALTERED")]
    public string ISCAPVATTAXALTERED { get; set; }

    [XmlElement(ElementName = "ISCAPVATNOTCLAIMED")]
    public string ISCAPVATNOTCLAIMED { get; set; }

    [XmlElement(ElementName = "AMOUNT")]
    public double AMOUNT { get; set; }

    [XmlElement(ElementName = "SERVICETAXDETAILS.LIST")]
    public object SERVICETAXDETAILSLIST { get; set; }

    [XmlElement(ElementName = "BANKALLOCATIONS.LIST")]
    public object BANKALLOCATIONSLIST { get; set; }

    [XmlElement(ElementName = "BILLALLOCATIONS.LIST")]
    public object BILLALLOCATIONSLIST { get; set; }

    [XmlElement(ElementName = "INTERESTCOLLECTION.LIST")]
    public object INTERESTCOLLECTIONLIST { get; set; }

    [XmlElement(ElementName = "OLDAUDITENTRIES.LIST")]
    public object OLDAUDITENTRIESLIST { get; set; }

    [XmlElement(ElementName = "ACCOUNTAUDITENTRIES.LIST")]
    public object ACCOUNTAUDITENTRIESLIST { get; set; }

    [XmlElement(ElementName = "AUDITENTRIES.LIST")]
    public object AUDITENTRIESLIST { get; set; }

    [XmlElement(ElementName = "INPUTCRALLOCS.LIST")]
    public object INPUTCRALLOCSLIST { get; set; }

    [XmlElement(ElementName = "DUTYHEADDETAILS.LIST")]
    public object DUTYHEADDETAILSLIST { get; set; }

    [XmlElement(ElementName = "EXCISEDUTYHEADDETAILS.LIST")]
    public object EXCISEDUTYHEADDETAILSLIST { get; set; }

    [XmlElement(ElementName = "RATEDETAILS.LIST")]
    public List<RATEDETAILSLIST> RATEDETAILSLIST { get; set; }

    [XmlElement(ElementName = "SUMMARYALLOCS.LIST")]
    public object SUMMARYALLOCSLIST { get; set; }

    [XmlElement(ElementName = "CENVATDUTYALLOCATIONS.LIST")]
    public object CENVATDUTYALLOCATIONSLIST { get; set; }

    [XmlElement(ElementName = "STPYMTDETAILS.LIST")]
    public object STPYMTDETAILSLIST { get; set; }

    [XmlElement(ElementName = "EXCISEPAYMENTALLOCATIONS.LIST")]
    public object EXCISEPAYMENTALLOCATIONSLIST { get; set; }

    [XmlElement(ElementName = "TAXBILLALLOCATIONS.LIST")]
    public object TAXBILLALLOCATIONSLIST { get; set; }

    [XmlElement(ElementName = "TAXOBJECTALLOCATIONS.LIST")]
    public object TAXOBJECTALLOCATIONSLIST { get; set; }

    [XmlElement(ElementName = "TDSEXPENSEALLOCATIONS.LIST")]
    public object TDSEXPENSEALLOCATIONSLIST { get; set; }

    [XmlElement(ElementName = "VATSTATUTORYDETAILS.LIST")]
    public object VATSTATUTORYDETAILSLIST { get; set; }

    [XmlElement(ElementName = "COSTTRACKALLOCATIONS.LIST")]
    public object COSTTRACKALLOCATIONSLIST { get; set; }

    [XmlElement(ElementName = "REFVOUCHERDETAILS.LIST")]
    public object REFVOUCHERDETAILSLIST { get; set; }

    [XmlElement(ElementName = "INVOICEWISEDETAILS.LIST")]
    public object INVOICEWISEDETAILSLIST { get; set; }

    [XmlElement(ElementName = "VATITCDETAILS.LIST")]
    public object VATITCDETAILSLIST { get; set; }

    [XmlElement(ElementName = "ADVANCETAXDETAILS.LIST")]
    public object ADVANCETAXDETAILSLIST { get; set; }

    [XmlElement(ElementName = "TAXTYPEALLOCATIONS.LIST")]
    public object TAXTYPEALLOCATIONSLIST { get; set; }
}

[XmlRoot(ElementName = "VOUCHER")]
public class TallyVoucher : IInvEntryXmlSerializable
{
    [XmlAttribute(AttributeName = "ACTION")]
    public string Action { get; set; } = "Create";

    [XmlAttribute(AttributeName = "OBJVIEW")]
    public string ObjView { get; set; } = "Accounting Voucher View";

    [XmlElement(ElementName = "OLDAUDITENTRYIDS.LIST")]
    public OLDAUDITENTRYIDSLIST OLDAUDITENTRYIDSLIST { get; set; }

    [XmlElement(ElementName = "DATE")]
    public string DATE { get; set; }

    [XmlElement(ElementName = "VCHSTATUSDATE")]
    public string VCHSTATUSDATE { get; set; }

    [XmlElement(ElementName = "GUID")]
    public string GUID { get; set; }

    [XmlElement(ElementName = "GSTREGISTRATIONTYPE")]
    public string GSTREGISTRATIONTYPE { get; set; }

    [XmlElement(ElementName = "STATENAME")]
    public string STATENAME { get; set; }

    [XmlElement(ElementName = "NARRATION")]
    public string NARRATION { get; set; }

    [XmlElement(ElementName = "COUNTRYOFRESIDENCE")]
    public string COUNTRYOFRESIDENCE { get; set; }

    // Payment - present
    // Receipt - abset
    [XmlElement(ElementName = "PLACEOFSUPPLY")]
    public string PLACEOFSUPPLY { get; set; }

    [XmlElement(ElementName = "PARTYNAME")]
    public string PARTYNAME { get; set; }

    [XmlElement(ElementName = "GSTREGISTRATION")]
    public GSTREGISTRATION GSTREGISTRATION { get; set; }

    [XmlElement(ElementName = "CMPGSTIN")]
    public string CMPGSTIN { get; set; }

    [XmlElement(ElementName = "VOUCHERTYPENAME")]
    public string VOUCHERTYPENAME { get; set; }

    [XmlElement(ElementName = "PARTYLEDGERNAME")]
    public string PARTYLEDGERNAME { get; set; }

    [XmlElement(ElementName = "VOUCHERNUMBER")]
    public string VOUCHERNUMBER { get; set; }

    [XmlElement(ElementName = "CMPGSTREGISTRATIONTYPE")]
    public string CMPGSTREGISTRATIONTYPE { get; set; }

    [XmlElement(ElementName = "CMPGSTSTATE")]
    public string CMPGSTSTATE { get; set; }

    [XmlElement(ElementName = "NUMBERINGSTYLE")]
    public string NUMBERINGSTYLE { get; set; }

    [XmlElement(ElementName = "CSTFORMISSUETYPE")]
    public string CSTFORMISSUETYPE { get; set; }

    [XmlElement(ElementName = "CSTFORMRECVTYPE")]
    public string CSTFORMRECVTYPE { get; set; }

    [XmlElement(ElementName = "FBTPAYMENTTYPE")]
    public string FBTPAYMENTTYPE { get; set; }

    [XmlElement(ElementName = "PERSISTEDVIEW")]
    public string PERSISTEDVIEW { get; set; }

    [XmlElement(ElementName = "VCHSTATUSTAXADJUSTMENT")]
    public string VCHSTATUSTAXADJUSTMENT { get; set; }

    [XmlElement(ElementName = "VCHSTATUSVOUCHERTYPE")]
    public string VCHSTATUSVOUCHERTYPE { get; set; }

    [XmlElement(ElementName = "VCHSTATUSTAXUNIT")]
    public string VCHSTATUSTAXUNIT { get; set; }

    [XmlElement(ElementName = "VCHGSTCLASS")]
    public string VCHGSTCLASS { get; set; }

    [XmlElement(ElementName = "DIFFACTUALQTY")]
    public string DIFFACTUALQTY { get; set; }

    [XmlElement(ElementName = "ISMSTFROMSYNC")]
    public string ISMSTFROMSYNC { get; set; }

    [XmlElement(ElementName = "ISDELETED")]
    public string ISDELETED { get; set; }

    [XmlElement(ElementName = "ISSECURITYONWHENENTERED")]
    public string ISSECURITYONWHENENTERED { get; set; }

    [XmlElement(ElementName = "ASORIGINAL")]
    public string ASORIGINAL { get; set; }

    [XmlElement(ElementName = "AUDITED")]
    public string AUDITED { get; set; }

    [XmlElement(ElementName = "ISCOMMONPARTY")]
    public string ISCOMMONPARTY { get; set; }

    [XmlElement(ElementName = "FORJOBCOSTING")]
    public string FORJOBCOSTING { get; set; }

    [XmlElement(ElementName = "ISOPTIONAL")]
    public string ISOPTIONAL { get; set; }

    [XmlElement(ElementName = "EFFECTIVEDATE")]
    public int EFFECTIVEDATE { get; set; }

    [XmlElement(ElementName = "USEFOREXCISE")]
    public string USEFOREXCISE { get; set; }

    [XmlElement(ElementName = "ISFORJOBWORKIN")]
    public string ISFORJOBWORKIN { get; set; }

    [XmlElement(ElementName = "ALLOWCONSUMPTION")]
    public string ALLOWCONSUMPTION { get; set; }

    [XmlElement(ElementName = "USEFORINTEREST")]
    public string USEFORINTEREST { get; set; }

    [XmlElement(ElementName = "USEFORGAINLOSS")]
    public string USEFORGAINLOSS { get; set; }

    [XmlElement(ElementName = "USEFORGODOWNTRANSFER")]
    public string USEFORGODOWNTRANSFER { get; set; }

    [XmlElement(ElementName = "USEFORCOMPOUND")]
    public string USEFORCOMPOUND { get; set; }

    [XmlElement(ElementName = "USEFORSERVICETAX")]
    public string USEFORSERVICETAX { get; set; }

    [XmlElement(ElementName = "ISREVERSECHARGEAPPLICABLE")]
    public string ISREVERSECHARGEAPPLICABLE { get; set; }

    [XmlElement(ElementName = "ISSYSTEM")]
    public string ISSYSTEM { get; set; }

    [XmlElement(ElementName = "ISFETCHEDONLY")]
    public string ISFETCHEDONLY { get; set; }

    [XmlElement(ElementName = "ISGSTOVERRIDDEN")]
    public string ISGSTOVERRIDDEN { get; set; }

    [XmlElement(ElementName = "ISCANCELLED")]
    public string ISCANCELLED { get; set; }

    [XmlElement(ElementName = "ISONHOLD")]
    public string ISONHOLD { get; set; }

    [XmlElement(ElementName = "ISSUMMARY")]
    public string ISSUMMARY { get; set; }

    [XmlElement(ElementName = "ISECOMMERCESUPPLY")]
    public string ISECOMMERCESUPPLY { get; set; }

    [XmlElement(ElementName = "ISBOENOTAPPLICABLE")]
    public string ISBOENOTAPPLICABLE { get; set; }

    [XmlElement(ElementName = "ISGSTSECSEVENAPPLICABLE")]
    public string ISGSTSECSEVENAPPLICABLE { get; set; }

    [XmlElement(ElementName = "IGNOREEINVVALIDATION")]
    public string IGNOREEINVVALIDATION { get; set; }

    [XmlElement(ElementName = "CMPGSTISOTHTERRITORYASSESSEE")]
    public string CMPGSTISOTHTERRITORYASSESSEE { get; set; }

    [XmlElement(ElementName = "PARTYGSTISOTHTERRITORYASSESSEE")]
    public string PARTYGSTISOTHTERRITORYASSESSEE { get; set; }

    [XmlElement(ElementName = "IRNJSONEXPORTED")]
    public string IRNJSONEXPORTED { get; set; }

    [XmlElement(ElementName = "IRNCANCELLED")]
    public string IRNCANCELLED { get; set; }

    [XmlElement(ElementName = "IGNOREGSTCONFLICTINMIG")]
    public string IGNOREGSTCONFLICTINMIG { get; set; }

    [XmlElement(ElementName = "ISOPBALTRANSACTION")]
    public string ISOPBALTRANSACTION { get; set; }

    [XmlElement(ElementName = "IGNOREGSTFORMATVALIDATION")]
    public string IGNOREGSTFORMATVALIDATION { get; set; }

    [XmlElement(ElementName = "ISELIGIBLEFORITC")]
    public string ISELIGIBLEFORITC { get; set; }

    [XmlElement(ElementName = "UPDATESUMMARYVALUES")]
    public string UPDATESUMMARYVALUES { get; set; }

    [XmlElement(ElementName = "ISEWAYBILLAPPLICABLE")]
    public string ISEWAYBILLAPPLICABLE { get; set; }

    [XmlElement(ElementName = "ISDELETEDRETAINED")]
    public string ISDELETEDRETAINED { get; set; }

    [XmlElement(ElementName = "ISNULL")]
    public string ISNULL { get; set; }

    [XmlElement(ElementName = "ISEXCISEVOUCHER")]
    public string ISEXCISEVOUCHER { get; set; }

    [XmlElement(ElementName = "EXCISETAXOVERRIDE")]
    public string EXCISETAXOVERRIDE { get; set; }

    [XmlElement(ElementName = "USEFORTAXUNITTRANSFER")]
    public string USEFORTAXUNITTRANSFER { get; set; }

    [XmlElement(ElementName = "ISEXER1NOPOVERWRITE")]
    public string ISEXER1NOPOVERWRITE { get; set; }

    [XmlElement(ElementName = "ISEXF2NOPOVERWRITE")]
    public string ISEXF2NOPOVERWRITE { get; set; }

    [XmlElement(ElementName = "ISEXER3NOPOVERWRITE")]
    public string ISEXER3NOPOVERWRITE { get; set; }

    [XmlElement(ElementName = "IGNOREPOSVALIDATION")]
    public string IGNOREPOSVALIDATION { get; set; }

    [XmlElement(ElementName = "EXCISEOPENING")]
    public string EXCISEOPENING { get; set; }

    [XmlElement(ElementName = "USEFORFINALPRODUCTION")]
    public string USEFORFINALPRODUCTION { get; set; }

    [XmlElement(ElementName = "ISTDSOVERRIDDEN")]
    public string ISTDSOVERRIDDEN { get; set; }

    [XmlElement(ElementName = "ISTCSOVERRIDDEN")]
    public string ISTCSOVERRIDDEN { get; set; }

    [XmlElement(ElementName = "ISTDSTCSCASHVCH")]
    public string ISTDSTCSCASHVCH { get; set; }

    [XmlElement(ElementName = "INCLUDEADVPYMTVCH")]
    public string INCLUDEADVPYMTVCH { get; set; }

    [XmlElement(ElementName = "ISSUBWORKSCONTRACT")]
    public string ISSUBWORKSCONTRACT { get; set; }

    [XmlElement(ElementName = "ISVATOVERRIDDEN")]
    public string ISVATOVERRIDDEN { get; set; }

    [XmlElement(ElementName = "IGNOREORIGVCHDATE")]
    public string IGNOREORIGVCHDATE { get; set; }

    [XmlElement(ElementName = "ISVATPAIDATCUSTOMS")]
    public string ISVATPAIDATCUSTOMS { get; set; }

    [XmlElement(ElementName = "ISDECLAREDTOCUSTOMS")]
    public string ISDECLAREDTOCUSTOMS { get; set; }

    [XmlElement(ElementName = "VATADVANCEPAYMENT")]
    public string VATADVANCEPAYMENT { get; set; }

    [XmlElement(ElementName = "VATADVPAY")]
    public string VATADVPAY { get; set; }

    [XmlElement(ElementName = "ISCSTDELCAREDGOODSSALES")]
    public string ISCSTDELCAREDGOODSSALES { get; set; }

    [XmlElement(ElementName = "ISVATRESTAXINV")]
    public string ISVATRESTAXINV { get; set; }

    [XmlElement(ElementName = "ISSERVICETAXOVERRIDDEN")]
    public string ISSERVICETAXOVERRIDDEN { get; set; }

    [XmlElement(ElementName = "ISISDVOUCHER")]
    public string ISISDVOUCHER { get; set; }

    [XmlElement(ElementName = "ISEXCISEOVERRIDDEN")]
    public string ISEXCISEOVERRIDDEN { get; set; }

    [XmlElement(ElementName = "ISEXCISESUPPLYVCH")]
    public string ISEXCISESUPPLYVCH { get; set; }

    [XmlElement(ElementName = "GSTNOTEXPORTED")]
    public string GSTNOTEXPORTED { get; set; }

    [XmlElement(ElementName = "IGNOREGSTINVALIDATION")]
    public string IGNOREGSTINVALIDATION { get; set; }

    [XmlElement(ElementName = "ISGSTREFUND")]
    public string ISGSTREFUND { get; set; }

    [XmlElement(ElementName = "OVRDNEWAYBILLAPPLICABILITY")]
    public string OVRDNEWAYBILLAPPLICABILITY { get; set; }

    [XmlElement(ElementName = "ISVATPRINCIPALACCOUNT")]
    public string ISVATPRINCIPALACCOUNT { get; set; }

    [XmlElement(ElementName = "VCHSTATUSISVCHNUMUSED")]
    public string VCHSTATUSISVCHNUMUSED { get; set; }

    [XmlElement(ElementName = "VCHGSTSTATUSISINCLUDED")]
    public string VCHGSTSTATUSISINCLUDED { get; set; }

    [XmlElement(ElementName = "VCHGSTSTATUSISUNCERTAIN")]
    public string VCHGSTSTATUSISUNCERTAIN { get; set; }

    [XmlElement(ElementName = "VCHGSTSTATUSISEXCLUDED")]
    public string VCHGSTSTATUSISEXCLUDED { get; set; }

    [XmlElement(ElementName = "VCHGSTSTATUSISAPPLICABLE")]
    public string VCHGSTSTATUSISAPPLICABLE { get; set; }

    [XmlElement(ElementName = "VCHGSTSTATUSISGSTR2BRECONCILED")]
    public string VCHGSTSTATUSISGSTR2BRECONCILED { get; set; }

    [XmlElement(ElementName = "VCHGSTSTATUSISGSTR2BONLYINPORTAL")]
    public string VCHGSTSTATUSISGSTR2BONLYINPORTAL { get; set; }

    [XmlElement(ElementName = "VCHGSTSTATUSISGSTR2BONLYINBOOKS")]
    public string VCHGSTSTATUSISGSTR2BONLYINBOOKS { get; set; }

    [XmlElement(ElementName = "VCHGSTSTATUSISGSTR2BMISMATCH")]
    public string VCHGSTSTATUSISGSTR2BMISMATCH { get; set; }

    [XmlElement(ElementName = "VCHGSTSTATUSISGSTR2BINDIFFPERIOD")]
    public string VCHGSTSTATUSISGSTR2BINDIFFPERIOD { get; set; }

    [XmlElement(ElementName = "VCHGSTSTATUSISRETEFFDATEOVERRDN")]
    public string VCHGSTSTATUSISRETEFFDATEOVERRDN { get; set; }

    [XmlElement(ElementName = "VCHGSTSTATUSISOVERRDN")]
    public string VCHGSTSTATUSISOVERRDN { get; set; }

    [XmlElement(ElementName = "VCHGSTSTATUSISSTATINDIFFDATE")]
    public string VCHGSTSTATUSISSTATINDIFFDATE { get; set; }

    [XmlElement(ElementName = "VCHGSTSTATUSISRETINDIFFDATE")]
    public string VCHGSTSTATUSISRETINDIFFDATE { get; set; }

    [XmlElement(ElementName = "VCHGSTSTATUSMAINSECTIONEXCLUDED")]
    public string VCHGSTSTATUSMAINSECTIONEXCLUDED { get; set; }

    [XmlElement(ElementName = "VCHGSTSTATUSISBRANCHTRANSFEROUT")]
    public string VCHGSTSTATUSISBRANCHTRANSFEROUT { get; set; }

    [XmlElement(ElementName = "VCHGSTSTATUSISSYSTEMSUMMARY")]
    public string VCHGSTSTATUSISSYSTEMSUMMARY { get; set; }

    [XmlElement(ElementName = "VCHSTATUSISUNREGISTEREDRCM")]
    public string VCHSTATUSISUNREGISTEREDRCM { get; set; }

    [XmlElement(ElementName = "VCHSTATUSISOPTIONAL")]
    public string VCHSTATUSISOPTIONAL { get; set; }

    [XmlElement(ElementName = "VCHSTATUSISCANCELLED")]
    public string VCHSTATUSISCANCELLED { get; set; }

    [XmlElement(ElementName = "VCHSTATUSISDELETED")]
    public string VCHSTATUSISDELETED { get; set; }

    [XmlElement(ElementName = "VCHSTATUSISOPENINGBALANCE")]
    public string VCHSTATUSISOPENINGBALANCE { get; set; }

    [XmlElement(ElementName = "VCHSTATUSISFETCHEDONLY")]
    public string VCHSTATUSISFETCHEDONLY { get; set; }

    [XmlElement(ElementName = "PAYMENTLINKHASMULTIREF")]
    public string PAYMENTLINKHASMULTIREF { get; set; }

    [XmlElement(ElementName = "ISSHIPPINGWITHINSTATE")]
    public string ISSHIPPINGWITHINSTATE { get; set; }

    [XmlElement(ElementName = "ISOVERSEASTOURISTTRANS")]
    public string ISOVERSEASTOURISTTRANS { get; set; }

    [XmlElement(ElementName = "ISDESIGNATEDZONEPARTY")]
    public string ISDESIGNATEDZONEPARTY { get; set; }

    [XmlElement(ElementName = "HASCASHFLOW")]
    public string HASCASHFLOW { get; set; }

    [XmlElement(ElementName = "ISPOSTDATED")]
    public string ISPOSTDATED { get; set; }

    [XmlElement(ElementName = "USETRACKINGNUMBER")]
    public string USETRACKINGNUMBER { get; set; }

    [XmlElement(ElementName = "ISINVOICE")]
    public string ISINVOICE { get; set; }

    [XmlElement(ElementName = "MFGJOURNAL")]
    public string MFGJOURNAL { get; set; }

    [XmlElement(ElementName = "HASDISCOUNTS")]
    public string HASDISCOUNTS { get; set; }

    [XmlElement(ElementName = "ASPAYSLIP")]
    public string ASPAYSLIP { get; set; }

    [XmlElement(ElementName = "ISCOSTCENTRE")]
    public string ISCOSTCENTRE { get; set; }

    [XmlElement(ElementName = "ISSTXNONREALIZEDVCH")]
    public string ISSTXNONREALIZEDVCH { get; set; }

    [XmlElement(ElementName = "ISEXCISEMANUFACTURERON")]
    public string ISEXCISEMANUFACTURERON { get; set; }

    [XmlElement(ElementName = "ISBLANKCHEQUE")]
    public string ISBLANKCHEQUE { get; set; }

    [XmlElement(ElementName = "ISVOID")]
    public string ISVOID { get; set; }

    [XmlElement(ElementName = "ORDERLINESTATUS")]
    public string ORDERLINESTATUS { get; set; }

    [XmlElement(ElementName = "VATISAGNSTCANCSALES")]
    public string VATISAGNSTCANCSALES { get; set; }

    [XmlElement(ElementName = "VATISPURCEXEMPTED")]
    public string VATISPURCEXEMPTED { get; set; }

    [XmlElement(ElementName = "ISVATRESTAXINVOICE")]
    public string ISVATRESTAXINVOICE { get; set; }

    [XmlElement(ElementName = "VATISASSESABLECALCVCH")]
    public string VATISASSESABLECALCVCH { get; set; }

    [XmlElement(ElementName = "ISVATDUTYPAID")]
    public string ISVATDUTYPAID { get; set; }

    [XmlElement(ElementName = "ISDELIVERYSAMEASCONSIGNEE")]
    public string ISDELIVERYSAMEASCONSIGNEE { get; set; }

    [XmlElement(ElementName = "ISDISPATCHSAMEASCONSIGNOR")]
    public string ISDISPATCHSAMEASCONSIGNOR { get; set; }

    [XmlElement(ElementName = "ISDELETEDVCHRETAINED")]
    public string ISDELETEDVCHRETAINED { get; set; }

    [XmlElement(ElementName = "CHANGEVCHMODE")]
    public string CHANGEVCHMODE { get; set; }

    [XmlElement(ElementName = "RESETIRNQRCODE")]
    public string RESETIRNQRCODE { get; set; }

    [XmlElement(ElementName = "ALTERID")]
    public int ALTERID { get; set; }

    [XmlElement(ElementName = "MASTERID")]
    public int MASTERID { get; set; }

    [XmlElement(ElementName = "VOUCHERKEY")]
    public double VOUCHERKEY { get; set; }

    [XmlElement(ElementName = "VOUCHERRETAINKEY")]
    public int VOUCHERRETAINKEY { get; set; }

    [XmlElement(ElementName = "VOUCHERNUMBERSERIES")]
    public string VOUCHERNUMBERSERIES { get; set; }

    [XmlElement(ElementName = "EWAYBILLDETAILS.LIST")]
    public object EWAYBILLDETAILSLIST { get; set; }

    [XmlElement(ElementName = "EXCLUDEDTAXATIONS.LIST")]
    public object EXCLUDEDTAXATIONSLIST { get; set; }

    [XmlElement(ElementName = "OLDAUDITENTRIES.LIST")]
    public object OLDAUDITENTRIESLIST { get; set; }

    [XmlElement(ElementName = "ACCOUNTAUDITENTRIES.LIST")]
    public object ACCOUNTAUDITENTRIESLIST { get; set; }

    [XmlElement(ElementName = "AUDITENTRIES.LIST")]
    public object AUDITENTRIESLIST { get; set; }

    [XmlElement(ElementName = "DUTYHEADDETAILS.LIST")]
    public object DUTYHEADDETAILSLIST { get; set; }

    [XmlElement(ElementName = "GSTADVADJDETAILS.LIST")]
    public object GSTADVADJDETAILSLIST { get; set; }

    [XmlElement(ElementName = "CONTRITRANS.LIST")]
    public object CONTRITRANSLIST { get; set; }

    [XmlElement(ElementName = "EWAYBILLERRORLIST.LIST")]
    public object EWAYBILLERRORLISTLIST { get; set; }

    [XmlElement(ElementName = "IRNERRORLIST.LIST")]
    public object IRNERRORLISTLIST { get; set; }

    [XmlElement(ElementName = "HARYANAVAT.LIST")]
    public object HARYANAVATLIST { get; set; }

    [XmlElement(ElementName = "SUPPLEMENTARYDUTYHEADDETAILS.LIST")]
    public object SUPPLEMENTARYDUTYHEADDETAILSLIST { get; set; }

    [XmlElement(ElementName = "INVOICEDELNOTES.LIST")]
    public object INVOICEDELNOTESLIST { get; set; }

    [XmlElement(ElementName = "INVOICEORDERLIST.LIST")]
    public object INVOICEORDERLISTLIST { get; set; }

    [XmlElement(ElementName = "INVOICEINDENTLIST.LIST")]
    public object INVOICEINDENTLISTLIST { get; set; }

    [XmlElement(ElementName = "ATTENDANCEENTRIES.LIST")]
    public object ATTENDANCEENTRIESLIST { get; set; }

    [XmlElement(ElementName = "ORIGINVOICEDETAILS.LIST")]
    public object ORIGINVOICEDETAILSLIST { get; set; }

    [XmlElement(ElementName = "INVOICEEXPORTLIST.LIST")]
    public object INVOICEEXPORTLISTLIST { get; set; }

    [XmlElement(ElementName = "ALLLEDGERENTRIES.LIST")]
    public List<ALLLEDGERENTRIESLIST> ALLLEDGERENTRIESLIST { get; set; }

    [XmlElement(ElementName = "GST.LIST")]
    public object GSTLIST { get; set; }

    [XmlElement(ElementName = "STKJRNLADDLCOSTDETAILS.LIST")]
    public object STKJRNLADDLCOSTDETAILSLIST { get; set; }

    [XmlElement(ElementName = "PAYROLLMODEOFPAYMENT.LIST")]
    public object PAYROLLMODEOFPAYMENTLIST { get; set; }

    [XmlElement(ElementName = "ATTDRECORDS.LIST")]
    public object ATTDRECORDSLIST { get; set; }

    [XmlElement(ElementName = "GSTEWAYCONSIGNORADDRESS.LIST")]
    public object GSTEWAYCONSIGNORADDRESSLIST { get; set; }

    [XmlElement(ElementName = "GSTEWAYCONSIGNEEADDRESS.LIST")]
    public object GSTEWAYCONSIGNEEADDRESSLIST { get; set; }

    [XmlElement(ElementName = "TEMPGSTRATEDETAILS.LIST")]
    public object TEMPGSTRATEDETAILSLIST { get; set; }

    [XmlElement(ElementName = "TEMPGSTADVADJUSTED.LIST")]
    public object TEMPGSTADVADJUSTEDLIST { get; set; }

    [XmlElement(ElementName = "GSTBUYERADDRESS.LIST")]
    public object GSTBUYERADDRESSLIST { get; set; }

    [XmlElement(ElementName = "GSTCONSIGNEEADDRESS.LIST")]
    public object GSTCONSIGNEEADDRESSLIST { get; set; }

    [XmlAttribute(AttributeName = "REMOTEID")]
    public string REMOTEID { get; set; }

    [XmlAttribute(AttributeName = "VCHKEY")]
    public string VCHKEY { get; set; }

    [XmlAttribute(AttributeName = "VCHTYPE")]
    public string VCHTYPE { get; set; }

    [XmlAttribute(AttributeName = "ACTION")]
    public string ACTION { get; set; }

    [XmlAttribute(AttributeName = "OBJVIEW")]
    public string OBJVIEW { get; set; }

    [XmlText]
    public string Text { get; set; }
}

[XmlRoot(ElementName = "TALLYMESSAGE")]
public class TALLYMESSAGE : IInvEntryXmlSerializable
{

    [XmlElement(ElementName = "VOUCHER")]
    public TallyVoucher VOUCHER { get; set; }

    [XmlAttribute(AttributeName = "UDF")]
    public string UDF { get; set; }

    [XmlText]
    public string Text { get; set; }

    [XmlElement(ElementName = "COMPANY")]
    public COMPANY COMPANY { get; set; }
}

[XmlRoot(ElementName = "REMOTECMPINFO.LIST")]
public class REMOTECMPINFOLIST : IInvEntryXmlSerializable
{

    [XmlElement(ElementName = "NAME")]
    public string NAME { get; set; }

    [XmlElement(ElementName = "REMOTECMPNAME")]
    public string REMOTECMPNAME { get; set; }

    [XmlElement(ElementName = "REMOTECMPSTATE")]
    public string REMOTECMPSTATE { get; set; }

    [XmlAttribute(AttributeName = "MERGE")]
    public string MERGE { get; set; }

    [XmlText]
    public string Text { get; set; }
}

[XmlRoot(ElementName = "COMPANY")]
public class COMPANY : IInvEntryXmlSerializable
{

    [XmlElement(ElementName = "REMOTECMPINFO.LIST")]
    public REMOTECMPINFOLIST REMOTECMPINFOLIST { get; set; }
}

[XmlRoot(ElementName = "REQUESTDATA")]
public class REQUESTDATA : IInvEntryXmlSerializable
{

    [XmlElement(ElementName = "TALLYMESSAGE")]
    public List<TALLYMESSAGE> TALLYMESSAGE { get; set; }
}

[XmlRoot(ElementName = "IMPORTDATA")]
public class IMPORTDATA : IInvEntryXmlSerializable
{

    [XmlElement(ElementName = "REQUESTDESC")]
    public REQUESTDESC REQUESTDESC { get; set; }

    [XmlElement(ElementName = "REQUESTDATA")]
    public REQUESTDATA REQUESTDATA { get; set; }
}

[XmlRoot(ElementName = "BODY")]
public class BODY : IInvEntryXmlSerializable
{

    [XmlElement(ElementName = "IMPORTDATA")]
    public IMPORTDATA IMPORTDATA { get; set; }
}

[XmlRoot(ElementName = "ENVELOPE")]
public class ENVELOPE : IInvEntryXmlSerializable
{

    [XmlElement(ElementName = "HEADER")]
    public HEADER HEADER { get; set; }

    [XmlElement(ElementName = "BODY")]
    public BODY BODY { get; set; }
}

