using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
