using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
