using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace OTPServer.XML.OTPPacket
{
    [Serializable]
    class ProcessIdentifier
    {
        public const int NONE = 0;

        private int _ID = 0;
        public int ID
        {
            get { return this._ID; }
            set { this._ID = value; }
        }

        public bool SetFromXMLReader(XmlTextReader xmlReader)
        {
            bool success = true;
            this._ID = xmlReader.ReadContentAsInt();
            return success;
        }
    }
}
