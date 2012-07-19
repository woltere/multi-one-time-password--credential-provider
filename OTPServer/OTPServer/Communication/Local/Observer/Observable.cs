using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OTPServer.Communication.Local.Observer
{
    class Observable
    {
        private List<Observer> _Observers = new List<Observer>();

        public void Attach(Observer observer)
        {
            _Observers.Add(observer);
        }

        public void Detach(Observer observer)
        {
            _Observers.Remove(observer);
        }

        public void Notify()
        {
            foreach (Observer o in _Observers)
            {
                o.Update();
            }
        }
    }
}
