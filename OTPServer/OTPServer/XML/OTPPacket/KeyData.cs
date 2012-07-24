using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace OTPServer.XML.OTPPacket
{
    [Serializable]
    public class KeyData
    {
        public enum TYPE : int
        {
            NONE    = 0,
            RSA     = 1,

            PROTOCOL_VERSION_1_END = 2,
        }

        private TYPE _Type;
        public TYPE Type
        {
            get { return this._Type; }
            set { this._Type = value; }
        }

        private int _Modulus;
        public int Modulus
        {
            get { return this._Modulus; }
            set { this._Modulus = value; }
        }

        private int _Exponent;
        public int Exponent
        {
            get { return this._Exponent; }
            set { this._Exponent = value; }
        }

        public KeyData() 
        {
            Type = TYPE.NONE;
            Modulus  = 0;
            Exponent = 0;
        }

        ~KeyData() 
        {
            Type = TYPE.NONE;
            Modulus  = 0;
            Exponent = 0;
        }

        private void CleanUp()
        {
            Type = TYPE.NONE;
            Modulus  = 0;
            Exponent = 0;
        }

        public bool SetFromXMLReader(XmlReader xmlReader)
        {
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

        private bool ParseElementNode(XmlReader xmlReader)
        {
            bool success = true;

            if (xmlReader.Name.Equals("RSAKeyValue"))
            {
                Type = TYPE.RSA;
                goto Return;
            }
            else if (xmlReader.Name.Equals("Exponent"))
            {
                Exponent = XmlConvert.ToInt32(xmlReader.Value);
                goto Return;
            }
            else if (xmlReader.Name.Equals("Modulus"))
            {
                Modulus = XmlConvert.ToInt32(xmlReader.Value);
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

        public void ToXmlString(ref XmlWriter xmlWriter)
        {
            // Server should not send any KeyData
        }
    }
}
