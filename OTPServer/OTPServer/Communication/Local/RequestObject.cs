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
    class RequestObject<T_REQ, T_REP> : Observer.Observable
    {
        private T_REQ _Request;
        public T_REQ Request
        {
            get { return this._Request; }
            set { this._Request = value; }
        }

        private T_REP _Response;
        public T_REP Response
        {
            get { return this._Response; }
            set { this._Response = value; }
        }

        public RequestObject(Observer.Observer observer, T_REQ reqObj, T_REP repObj)
            : this(reqObj, repObj)
        {            
            if (observer != null)
                this.Attach(observer);
        }

        public RequestObject(T_REQ reqObj, T_REP repObj)
        {
            this._Request = reqObj;
            this._Response = repObj;
        }

        ~RequestObject()
        {
            this._Request = default(T_REQ);
            this._Response = default(T_REP);
        }
    }
}
