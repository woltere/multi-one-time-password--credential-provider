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


namespace OTPServer.Communication.Local
{
    abstract class ResponseObject
    {
        private bool _SimpleResponse = false;
        public bool SimpleResponse
        {
            get { return _SimpleResponse; }
            set { _SimpleResponse = value; }
        }

        /*
        private Object _ComplexResponse = new Object();
        public Object ComplexResponse
        {
            get { return this._ComplexResponse; }
            set { this._ComplexResponse = value; }
        }
        */
    }
}
