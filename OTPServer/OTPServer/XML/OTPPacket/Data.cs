using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace OTPServer.XML.OTPPacket
{
    class Data : ISerializable
    {
        public enum TYPE
        {
            NONE        = 0,
            USERNAME    = 1,
            OTP         = 1,
            CERTIFICATE = 1
        }

        private TYPE _Type;
        public TYPE Type
        {
            get { return this._Type; }
            set { this._Type = value; }
        }

        private Object _Content = null;
        public Object Content
        {
            get { return this._Content; }
            set { this._Content = value; }
        }

        public string Username
        {
            set
            {
                Content = value;
                Type = TYPE.USERNAME;
            }

            get { return (string)Content; }
        }

        public string OneTimePassword
        {
            set
            {
                Content = value;
                Type = TYPE.OTP;
            }

            get { return (string)Content; }
        }

        public string Certificate
        {
            set
            {
                Content = value;
                Type = TYPE.CERTIFICATE;
            }

            get { return (string)Content; }
        }

        public Data() 
        {
            Type = TYPE.NONE;
        }

        ~Data() 
        {
            Content = null;
            Type = TYPE.NONE;
        }

        private void CleanUp()
        {
            Content = null;
            Type = TYPE.NONE;
        }

        public bool SetFromXMLReader(XmlTextReader xmlReader)
        {
            Content = xmlReader.ReadContentAsObject();
            bool success = parseAttributes(xmlReader);            

            if (!success)
                CleanUp();

            return success;
        }

        private bool ParseAttributes(XmlTextReader xmlReader)
        {
            bool success = true;

            int attributeCount = xmlReader.AttributeCount;
            for (int i = 0; i < attributeCount; i++)
            {
                xmlReader.MoveToAttribute(i);
                if (xmlReader.Name.Equals("type"))
                {
                    if (xmlReader.Value.Equals("USERNAME"))
                        Type = TYPE.USERNAME;
                    else if (xmlReader.Value.Equals("OTP"))
                        Type = TYPE.OTP;
                    else if (xmlReader.Value.Equals("CERTIFICATE"))
                        Type = TYPE.CERTIFICATE;
                    goto Return;
                }
            }
        Return:
            return success;
        }
    }
}
