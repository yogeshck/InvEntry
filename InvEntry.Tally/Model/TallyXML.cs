using InvEntry.Utils;
using System.Xml.Serialization;

namespace InvEntry.Tally.Model;

public enum TallyRequestEnum
{
    Import, // Set
    Export, // Get
    Execute // Run
}

public enum TallyDataTypeEnum // => ID
{
    Data, // Name of request / Report
    Collection, // Name of the Collection
    Object, // Object ID / Name of Object 
    Action, // Action to be performed
    Function // Function to be executed
}

[XmlRoot(ElementName = "HEADER")]
public class TallyHeader : IInvEntryXmlSerializable
{

    [XmlElement(ElementName = "VERSION")]
    public int Version { get; set; } = 1;

    [XmlElement(ElementName = "TALLYREQUEST")]
    public TallyRequestEnum TallyRequest { get; set; }

    [XmlElement(ElementName = "TYPE")]
    public TallyDataTypeEnum Type { get; set; }

    [XmlElement(ElementName = "ID")]
    public string ID { get; set; }
}

[XmlRoot(ElementName = "BODY")]
public class TallyBody : IInvEntryXmlSerializable
{
    [XmlElement(ElementName = "DESC")]
    public TallyBodyDesc BodyDesc { get; set; }

    [XmlElement(ElementName = "DATA")]
    public object Data { get; set; }  
}

[XmlRoot(ElementName = "TALLYMESSAGE")]
public class TallyVoucherMessage : IInvEntryXmlSerializable
{
    [XmlElement(ElementName = "VOUCHER")]
    public List<TallyVoucher> Vouchers { get; set; }
}

[XmlRoot(ElementName = "DESC")]
public class TallyBodyDesc : IInvEntryXmlSerializable
{
    [XmlElement(ElementName = "STATICVARIABLES")]
    public TallyStaticVariables Variables { get; set; }
}

[XmlRoot(ElementName = "STATICVARIABLES")]
public class TallyStaticVariables : IInvEntryXmlSerializable
{

    [XmlElement(ElementName = "SVCURRENTCOMPANY")]
    public string SVCURRENTCOMPANY { get; set; }
}

[XmlRoot(ElementName = "REPEATVARIABLES")]
public class TallyRepeatVariables : IInvEntryXmlSerializable
{

}