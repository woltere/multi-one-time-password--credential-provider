using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Xml.Schema;

namespace OTPServer.XML.OTPPacket
{
    [Serializable]
    public class OTPPacket
    {
        public const int __PROTOCOL_VERSION = 1;

        private Message _Message;
        public Message Message
        {
            get { return this._Message; }
            set { this._Message = value; }
        }

        private ProcessIdentifier _ProcessIdentifier;
        public ProcessIdentifier ProcessIdentifier
        {
            get { return this._ProcessIdentifier; }
            set { this._ProcessIdentifier = value; }
        }

        private List<Data> _DataItems;
        public List<Data> DataItems
        {
            get { return this._DataItems; }
        }

        private KeyData _KeyData;
        public KeyData KeyData
        {
            get { return this._KeyData; }
            set { this._KeyData = value; }
        }

        public OTPPacket()
        {
            this._ProcessIdentifier = new ProcessIdentifier();
            this._Message   = new Message();
            this._DataItems = new List<Data>();
            this._KeyData   = new KeyData();
        }

        ~OTPPacket()
        {
            this._DataItems = null;
            this._Message   = null;
            this._KeyData   = null;
            this._ProcessIdentifier = null;
        }

        private void CleanUp()
        {
            this._DataItems = new List<Data>();
            this._Message   = new Message();
            this._KeyData   = new KeyData();
            this._ProcessIdentifier = new ProcessIdentifier();
        }

        public int AddDataItem(Data item)
        {
            this._DataItems.Add(item);
            return _DataItems.Count - 1;
        }

        public Data GetDataItemAt(int index)
        {
            return this._DataItems.ElementAt(index);
        }

        public void RemoveDataItemAt(int index)
        {
            this._DataItems.RemoveAt(index);
        }

        public string ToXMLString()
        {
            string xmlString = String.Empty;
            using (StringWriter stringWriter = new StringWriter())
            {
                XmlWriter xmlWriter = XmlWriter.Create(stringWriter);

                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("OTPPacket");
                xmlWriter.WriteAttributeString("version", __PROTOCOL_VERSION.ToString());

                this.ProcessIdentifier.ToXmlString(ref xmlWriter);
                this.Message.ToXmlString(ref xmlWriter);

                foreach (Data item in DataItems)
                    item.ToXmlString(ref xmlWriter);

                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();

                xmlWriter.Close();
                xmlString = stringWriter.ToString();

                File.AppendAllText("C:\\logloglog.log", " Content:( " + xmlString + " );\n");
            }
            return xmlString;
        }

        private XmlReaderSettings GetXMLReaderSettings(bool validateXml)
        {
            XmlReaderSettings xmlSettings = new XmlReaderSettings();

            xmlSettings.Schemas.Add("", "C:\\OTPPacketSchema.xsd");
            xmlSettings.ValidationType = (validateXml) ? ValidationType.Schema : ValidationType.None;
            xmlSettings.XmlResolver = null;

            return xmlSettings;
        }

        public bool SetFromXML(string xmlString, bool validateXml)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(xmlString);

            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                XmlTextReader xmlTextReader = new XmlTextReader(stream);
                return SetFromXML(xmlTextReader, validateXml);
            }
        }

        public bool SetFromXML(Stream xmlStream, bool validateXml)
        {
            string packet = ReadOTPPacketFromStream(xmlStream);
            return SetFromXML(packet, validateXml);
        }

        public bool SetFromXML(XmlTextReader xmlTextReader, bool validateXml)
        {
            XmlReaderSettings xmlSettings = GetXMLReaderSettings(validateXml);
            XmlReader xmlReader = XmlReader.Create(xmlTextReader, xmlSettings);

            bool success = true;
            try
            {
                File.AppendAllText("C:\\logloglog.log", "MAIN LOOP;\n");
                while (xmlReader.Read() && success)
                {
                    File.AppendAllText("C:\\logloglog.log", "MAIN LOOP CYCLE BEGIN;\n");
                    File.AppendAllText("C:\\logloglog.log", "xmlReader.Read() && success;\n");
                    File.AppendAllText("C:\\logloglog.log", "xmlReader.Name = " + xmlReader.Name + "; xmlReader.NodeType = " + xmlReader.NodeType.ToString() + ";\n");
                    XmlNodeType nType = xmlReader.NodeType;
                    if (nType == XmlNodeType.Element)
                    {
                        File.AppendAllText("C:\\logloglog.log", "nType == XmlNodeType.Element: " + xmlReader.Name + ";\n");
                        success = ParseElementNode(xmlReader);
                        continue;
                    }
                    File.AppendAllText("C:\\logloglog.log", "MAIN LOOP CYCLE END;\n");
                    File.AppendAllText("C:\\logloglog.log", "END MAIN LOOP;\n");
                }
                File.AppendAllText("C:\\logloglog.log", "END MAIN LOOP;\n");
            }
            catch (Exception e)
            {
                File.AppendAllText("C:\\logloglog.log", "Exception: " + e.Message + ";\n");
                // Validation failed
                success = false;
            }

            File.AppendAllText("C:\\logloglog.log", "OTPPacket.SetFromXML.success: " + success + ";\n");

            if (!success)
                CleanUp();

            File.AppendAllText("C:\\logloglog.log", "EXIT SetFromXML();\n");

            return success;
        }

        public static string ReadOTPPacketFromStream(Stream stream)
        {
            // Read the  message sent by the server.
            // The end of the message is signaled using the
            // "</OTPPacket>" marker.
            byte[] buffer = new byte[2048];
            StringBuilder messageData = new StringBuilder();
            int bytes = -1;
            do
            {
                try
                {
                    bytes = stream.Read(buffer, 0, buffer.Length);

                    // Use Decoder class to convert from bytes to UTF8
                    // in case a character spans two buffers.
                    Decoder decoder = Encoding.UTF8.GetDecoder();
                    char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                    decoder.GetChars(buffer, 0, bytes, chars, 0);
                    messageData.Append(chars);
                    // Check for EOF.
                    if (messageData.ToString().IndexOf("</OTPPacket>") != -1)
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            } while (bytes != 0);

            return messageData.ToString();
        }

        private bool ParseElementNode(XmlReader xmlReader)
        {
            bool success = true;

            if (xmlReader.Name.Equals("OTPPacket"))
            {
                File.AppendAllText("C:\\logloglog.log", "xmlReader.Name.Equals(\"OTPPacket\");\n");
                success = ParseAttributes(xmlReader);
                goto Return;
            }
            else if (xmlReader.Name.Equals("Message"))
            {
                File.AppendAllText("C:\\logloglog.log", "xmlReader.Name.Equals(\"Message\");\n");
                success = this._Message.SetFromXMLReader(xmlReader);
                goto Return;
            }
            else if (xmlReader.Name.Equals("Data"))
            {
                File.AppendAllText("C:\\logloglog.log", "xmlReader.Name.Equals(\"Data\");\n");
                Data item = new Data();
                success = item.SetFromXMLReader(xmlReader);

                this._DataItems.Add(item);
                goto Return;
            }
            else if (xmlReader.Name.Equals("KeyData"))
            {
                File.AppendAllText("C:\\logloglog.log", "xmlReader.Name.Equals(\"KeyData\");\n");
                success = this._KeyData.SetFromXMLReader(xmlReader);
                goto Return;
            }
            else
            {
                File.AppendAllText("C:\\logloglog.log", "ParseElementNode not recognized;\n");
                success = false;
                goto Return;
            }

        Return:
            File.AppendAllText("C:\\logloglog.log", "EXIT ParseElementNode();\n");
            return success;
        }

        private bool ParseAttributes(XmlReader xmlReader)
        {
            bool success = true;

            if (xmlReader.HasAttributes && xmlReader.MoveToFirstAttribute())
            {
                File.AppendAllText("C:\\logloglog.log", "xmlReader.HasAttributes && xmlReader.MoveToFirstAttribute();\n");
                do
                {
                    File.AppendAllText("C:\\logloglog.log", "ENTER LOOP;\n");
                    if (xmlReader.Name.Equals("version") && XmlConvert.ToInt32(xmlReader.Value) > __PROTOCOL_VERSION)
                    {
                        File.AppendAllText("C:\\logloglog.log", "xmlReader.Name.Equals(\"version\") && XmlConvert.ToInt32(" + xmlReader.Value + ") > __PROTOCOL_VERSION;\n");
                        success = false;
                        goto Return;
                    }
                } while (xmlReader.MoveToNextAttribute());
                File.AppendAllText("C:\\logloglog.log", "LEAVE LOOP;\n");
            }
            else
            {
                success = false;
                goto Return;
            }

        Return:
            File.AppendAllText("C:\\logloglog.log", "EXIT ParseAttributes();\n");
            return success;
        }
    }
}
