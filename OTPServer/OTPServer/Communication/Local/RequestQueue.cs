using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
