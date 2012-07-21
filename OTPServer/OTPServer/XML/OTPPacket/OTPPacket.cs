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

                xmlString = stringWriter.ToString();
                xmlWriter.Close();
            }
            return xmlString;
        }

        public bool ValidateXML(string xmlString)
        {
            byte[] byteArray = Encoding.ASCII.GetBytes(xmlString);
            MemoryStream stream = new MemoryStream(byteArray);
            return ValidateXML(stream);
        }

        public bool ValidateXML(Stream xmlStream)
        {
            XmlTextReader xmlReader = new XmlTextReader(xmlStream);
            return ValidateXML(xmlReader);
        }

        public bool ValidateXML(XmlTextReader xmlTextReader)
        {
            XmlReaderSettings xmlSettings = new XmlReaderSettings();
            xmlSettings.Schemas.Add("", "OTPPacketSchema.xsd");
            xmlSettings.ValidationType = ValidationType.Schema;
            xmlSettings.XmlResolver = null;

            XmlReader xmlReader = XmlReader.Create(xmlTextReader, xmlSettings);

            bool validationSuccess = true;
            try
            {
                while (xmlReader.Read()) { }
            }
            catch (XmlException)
            {
                validationSuccess = false;
            }

            return validationSuccess;
        }

        public bool SetFromXML(string xmlString, bool partialData)
        {
            byte[] byteArray = Encoding.ASCII.GetBytes(xmlString);
            MemoryStream stream = new MemoryStream(byteArray);
            return SetFromXML(stream, partialData);
        }

        public bool SetFromXML(Stream xmlStream, bool partialData)
        {
            XmlTextReader xmlReader = new XmlTextReader(xmlStream);
            return SetFromXML(xmlReader, partialData);
        }

        public bool SetFromXML(XmlTextReader xmlReader, bool partialData)
        {
            if (!partialData && !ValidateXML(xmlReader))
                return false;

            bool success = true;
            while (xmlReader.Read() && success)
            {
                XmlNodeType nType = xmlReader.NodeType;
                if (nType == XmlNodeType.Element)
                {
                    success = ParseElementNode(xmlReader);
                    continue;
                }
            }

            if (!success)
                CleanUp();

            return success;
        }

        private bool ParseElementNode(XmlTextReader xmlReader)
        {
            bool success = true;

            if (xmlReader.Name.Equals("OTPPacket"))
            {
                success = ParseAttributes(xmlReader);
                goto Return;
            }
            else if (xmlReader.Name.Equals("Message"))
            {
                success = this._Message.SetFromXMLReader(xmlReader);
                goto Return;
            }
            else if (xmlReader.Name.Equals("Data"))
            {
                Data item = new Data();
                success = item.SetFromXMLReader(xmlReader);

                this._DataItems.Add(item);
                goto Return;
            }
            else if (xmlReader.Name.Equals("KeyData"))
            {
                success = this._KeyData.SetFromXMLReader(xmlReader);
                goto Return;
            }
            else
            {
                success = false;
                goto Return;
            }

        Return:
            return success;
        }

        private bool ParseAttributes(XmlTextReader xmlReader)
        {
            bool success = true;

            if (xmlReader.HasAttributes && xmlReader.MoveToFirstAttribute())
            {
                do
                {
                    if (xmlReader.Name.Equals("version") && XmlConvert.ToChar(xmlReader.Value) > __PROTOCOL_VERSION)
                    {
                        success = false;
                        goto Return;
                    }
                } while (xmlReader.MoveToNextAttribute());
            }
            else
            {
                success = false;
                goto Return;
            }

        Return:
            return success;
        }
    }
}
