// See https://aka.ms/new-console-template for more information
using InvEntry.Utils;
using System.Xml.Serialization;


XmlExampleChild child1 = new()
{
    Name = "Mouli",
    Description = "Passport"
};

XmlExampleChild child2 = new()
{
    Name = "Arjun",
    Description = "Aadhar"
};

XmlExample parent = new()
{
    Name = "Sridevi",
    Description = "Passport/Aadhar",
    XmlExampleChilds = new()
};

parent.XmlExampleChilds.Add(child1);
parent.XmlExampleChilds.Add(child2);

var xmlstring = XMLUtil.SerializeToString(parent);
Console.WriteLine(xmlstring);

[XmlRoot(ElementName = "XmlRoot")]
public class XmlExample : IInvEntryXmlSerializable
{
    [XmlElement(ElementName = "PERSONNAME")]
    public string Name { get; set; }

    [XmlElement(ElementName = "PERSONID")]
    public string Description { get; set; }

    [XmlElement(ElementName = "CHILDREN")]
    public List<XmlExampleChild> XmlExampleChilds { get; set; }
}

[XmlRoot(ElementName = "CHILD")]
public class XmlExampleChild : IInvEntryXmlSerializable
{
    [XmlElement(ElementName = "CHILDNAME")]
    public string Name { get; set; }

    [XmlElement(ElementName = "CHILDID")]
    public string Description { get; set; }
}