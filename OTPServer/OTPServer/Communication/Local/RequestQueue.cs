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

using System.Collections.Generic;
using System.Linq;

namespace OTPServer.Communication.Local
{
    class RequestQueue<T_REQ, T_REP> : Observer.Observable
    {
        private List<RequestObject<T_REQ, T_REP>> _Requests = new List<RequestObject<T_REQ, T_REP>>();

        public bool Empty()
        {
            return (_Requests.Count == 0) ? true : false;
        }

        public RequestObject<T_REQ, T_REP> Enqueue(T_REQ request, T_REP response)
        {
            return EnqueueAndObserve(null, request, response);
        }

        public RequestObject<T_REQ, T_REP> EnqueueAndObserve(Observer.Observer observer, T_REQ request, T_REP response)
        {
            RequestObject<T_REQ, T_REP> requestObject = new RequestObject<T_REQ, T_REP>(observer, request, response);

            lock (_Requests)
                _Requests.Add(requestObject);

            this.Notify();

            return requestObject;
        }

        public RequestObject<T_REQ, T_REP> Dequeue()
        {
            RequestObject<T_REQ, T_REP> requestObject = null;

            lock (_Requests)
            {
                requestObject = _Requests.First();
                _Requests.Remove(_Requests.First());
            }

            return requestObject;
        }
    }
}
