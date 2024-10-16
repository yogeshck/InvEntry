using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InvEntry.Utils
{
    public interface IInvEntryXmlSerializable
    {

    }

    public class XMLUtil
    {
        public static T? Deserialize<T>(string xml) where T : class
        {
            T? result = default(T);

            if (!string.IsNullOrEmpty(xml))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                using (StringReader reader = new StringReader(xml))
                {
                    var obj = serializer.Deserialize(reader);
                    if (obj is not null)
                        result = obj as T;
                }
            }

            return result;
        }

        public static void SerializeToFile<T>(T obj, string filePath) where T : IInvEntryXmlSerializable
        {
            XmlSerializer ser = new XmlSerializer(typeof(T));
            TextWriter writer = new StreamWriter(filePath);
            ser.Serialize(writer, obj);
            writer.Close();
        }

        public static string SerializeToString<T>(T obj) where T : IInvEntryXmlSerializable
        {
            var serializer = new XmlSerializer(obj.GetType());

            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, obj);

                return writer.ToString();
            }
        }
    }
}
