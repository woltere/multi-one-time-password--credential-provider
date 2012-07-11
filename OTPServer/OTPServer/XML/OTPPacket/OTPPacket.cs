using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Xml.Schema;

namespace OTPServer.XML.OTPPacket
{
    class OTPPacket
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

        public OTPPacket()
        {
            this._ProcessIdentifier = new ProcessIdentifier();
            this._Message   = new Message();
            this._DataItems = new List<Data>();
        }

        ~OTPPacket()
        {
            this._DataItems = null;
            this._Message   = null;
            this._ProcessIdentifier = null;
        }

        private void CleanUp()
        {
            this._DataItems = new List<Data>();
            this._Message   = new Message();
            this._ProcessIdentifier = new ProcessIdentifier();
        }

        public int addDataItem(Data item)
        {
            this._DataItems.Add(item);
            return _DataItems.Count - 1;
        }

        public Data getDataItemAt(int index)
        {
            return this._DataItems.ElementAt(index);
        }

        public void removeDataItemAt(int index)
        {
            this._DataItems.RemoveAt(index);
        }

        public Stream convertToXML()
        {
            return null;
        }

        public bool validateXMLString(string xmlString)
        {
            byte[] byteArray = Encoding.ASCII.GetBytes(xmlString);
            MemoryStream stream = new MemoryStream(byteArray);
            return validateXMLStream(stream);
        }

        public bool validateXMLStream(Stream xmlStream)
        {
            XmlTextReader xmlReader = new XmlTextReader(xmlStream);
            return validateXMLReader(xmlReader);
        }

        public bool validateXMLReader(XmlTextReader xmlTextReader)
        {
            XmlValidatingReader xmlReader = new XmlValidatingReader(xmlTextReader);

            XmlSchemaCollection schemaCollection = new XmlSchemaCollection();
            schemaCollection.Add("", @"Schema\OTPPacketSchema.xsd");
            xmlReader.Schemas.Add(schemaCollection);

            bool validationSuccessfull = true;
            xmlReader.ValidationEventHandler += new ValidationEventHandler(
                (object sender, ValidationEventArgs e) => 
                    {
                        validationSuccessfull = false;
                    }
                );
            while (xmlReader.Read() && validationSuccessfull) ;

            return validationSuccessfull;
        }

        public bool setFromXMLString(string xmlString)
        {
            byte[] byteArray = Encoding.ASCII.GetBytes(xmlString);
            MemoryStream stream = new MemoryStream(byteArray);
            return setFromXMLStream(stream);
        }

        public bool setFromXMLStream(Stream xmlStream)
        {
            XmlTextReader xmlReader = new XmlTextReader(xmlStream);
            return setFromXMLReader(xmlReader);
        }

        public bool setFromXMLReader(XmlTextReader xmlReader)
        {
            if (!validateXMLReader(xmlReader))
                return false;

            bool success = true;
            while (xmlReader.Read() && success)
            {
                XmlNodeType nType = xmlReader.NodeType;
                if (nType == XmlNodeType.Element)
                {
                    success = parseElementNode(xmlReader);
                    continue;
                }
            }

            if (!success)
                CleanUp();

            return success;
        }

        private bool parseElementNode(XmlTextReader xmlReader)
        {
            bool success = true;

            if (xmlReader.Name.Equals("OTPPacket"))
            {
                success = parseAttributes(xmlReader);
                goto Return;
            }
            else if (xmlReader.Name.Equals("Message"))
            {
                success = this._Message.setFromXMLReader(xmlReader);
                goto Return;
            }
            else if (xmlReader.Name.Equals("Data"))
            {
                Data item = new Data();
                success = item.setFromXMLReader(xmlReader);

                this._DataItems.Add(item);
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

        private bool parseAttributes(XmlTextReader xmlReader)
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
