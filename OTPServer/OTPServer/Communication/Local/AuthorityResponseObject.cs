/* * * * * * * * * * * * * * * * * * * * *
**
** Copyright 2012 Dominik Pretzsch
** 
**    Licensed under the Apache License, Version 2.0 (the "License");
**    you may not use this file except in compliance with the License.
**    You may obtain a copy of the License at
** 
**        http://www.apache.org/licenses/LICENSE-2.0
** 
**    Unless required by applicable law or agreed to in writing, software
**    distributed under the License is distributed on an "AS IS" BASIS,
**    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
**    See the License for the specific language governing permissions and
**    limitations under the License.
**
** * * * * * * * * * * * * * * * * * * */

using OTPHelpers.XML.OTPPacket;

namespace OTPServer.Communication.Local
{
    class AuthorityResponseObject : ResponseObject
    {
        private AuthorityResponse _ComplexResponse = new AuthorityResponse();
        public AuthorityResponse ComplexResponse
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
