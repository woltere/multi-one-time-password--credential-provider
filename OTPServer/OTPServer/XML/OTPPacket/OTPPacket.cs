using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace OTPServer.XML.OTPPacket
{
    class OTPPacket
    {
        public static const int PROTOCOL_VERSION = 1;

        public readonly enum LEGAL_ELEMENTS
        {
            OTPPacket = 1, // Value is the protocol version where Name was introduced
            PID       = 1,
            Message   = 1,
            Data      = 1
        }

        public readonly enum LEGAL_ATTRIBUTES
        {
            version = 1
        }

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

        public ~OTPPacket()
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

        public bool setFromXML(XmlTextReader xmlReader)
        {
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

            bool thisIsTheRoot = true;
            bool itWasTheCorrectRoot = false;

            string[] legalElements = Enum.GetNames(typeof(LEGAL_ELEMENTS));

            if (!legalElements.Contains(xmlReader.Name))
            {
                success = false;
                goto Return;
            }

            if (xmlReader.Name.Equals("OTPPacket") && thisIsTheRoot)
            {
                thisIsTheRoot &= itWasTheCorrectRoot == true;

                success = parseAttributes(xmlReader);
                goto Return;
            }
            else if (xmlReader.Name.Equals("Message") && !thisIsTheRoot)
            {
                success = this._Message.setFromXML(xmlReader);
                goto Return;
            }
            else if (xmlReader.Name.Equals("Data") && !thisIsTheRoot)
            {
                Data item = new Data();
                success = item.setFromXML(xmlReader);

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

            string[] legalAttributes = Enum.GetNames(typeof(LEGAL_ATTRIBUTES));

            if (xmlReader.HasAttributes && xmlReader.MoveToFirstAttribute())
            {
                do
                {
                    if (!legalAttributes.Contains(xmlReader.Name))
                    {
                        success = false;
                        goto Return;
                    }
                    if (xmlReader.Name.Equals("version") && XmlConvert.ToChar(xmlReader.Value) > PROTOCOL_VERSION)
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
