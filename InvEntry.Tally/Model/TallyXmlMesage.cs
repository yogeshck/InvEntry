using InvEntry.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InvEntry.Tally.Model;

[XmlRoot(ElementName = "ENVELOPE")]
public class TallyXmlMesage : IInvEntryXmlSerializable
{
    [XmlElement(ElementName = "HEADER")]
    public TallyHeader HEADER;

    [XmlElement(ElementName = "BODY")]
    public TallyBody BODY;
}
