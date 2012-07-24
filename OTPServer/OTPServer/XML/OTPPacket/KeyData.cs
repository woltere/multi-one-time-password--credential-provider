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

        private string _Modulus;
        public byte[] Modulus
        {
            get { return Convert.FromBase64String(this._Modulus); }
            set { this._Modulus = Convert.ToBase64String(value); }
        }

        private string _Exponent;
        public byte[] Exponent
        {
            get { return Convert.FromBase64String(this._Exponent); }
            set { this._Exponent = Convert.ToBase64String(value); }
        }

        public KeyData() 
        {
            Type = TYPE.NONE;
            this._Modulus  = String.Empty;
            this._Exponent = String.Empty;
        }

        ~KeyData() 
        {
            Type = TYPE.NONE;
            this._Modulus = String.Empty;
            this._Exponent = String.Empty;
        }

        private void CleanUp()
        {
            Type = TYPE.NONE;
            this._Modulus = String.Empty;
            this._Exponent = String.Empty;
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
                this._Exponent = xmlReader.Value;
                goto Return;
            }
            else if (xmlReader.Name.Equals("Modulus"))
            {
                this._Modulus = xmlReader.Value;
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
            // Server should not send any KeyData, client should use this._Modulus and this._Exponent
        }
    }
}
