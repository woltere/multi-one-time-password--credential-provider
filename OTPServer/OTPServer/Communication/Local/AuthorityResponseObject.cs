using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OTPServer.XML.OTPPacket;

namespace OTPServer.Communication.Local
{
    class AuthorityResponseObject : ResponseObject
    {
        private override AuthorityResponse _ComplexResponse = new AuthorityResponse();
        public override AuthorityResponse ComplexResponse
        {
            get { return this._ComplexResponse; }
            set { this._ComplexResponse = value; }
        }

        public class AuthorityResponse
        {
            private int _ProcessIdentifier;
            public int ProcessIdentifier
            {
                get { return this._ProcessIdentifier; }
                set { this._ProcessIdentifier = value; }
            }

            private Message.STATUS _StatusCode;
            public Message.STATUS StatusCode
            {
                get { return this._StatusCode; }
                set { this._StatusCode = value; }
            }

            private string _TextMessage;
            public string TextMessage
            {
                get { return this._TextMessage; }
                set { this._TextMessage = value; }
            }

            /*
            private KeyData _KeyData;
            public KeyData KeyData
            {
                get { return this._KeyData; }
                set { this._KeyData = value; }
            }
            */
        }
    }
}
