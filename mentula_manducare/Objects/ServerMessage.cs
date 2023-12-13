using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mentula_manducare.Objects
{
    public class ServerMessage
    {
        private Stopwatch timer;
        public TimeSpan interval;
        public string message;

        public ServerMessage(string interval, string message)
        {
            this.interval = TimeSpan.Parse(interval);
            this.message = message;
            this.timer = Stopwatch.StartNew();
        }

        public bool Tick()
        {
            if (timer.Elapsed <= interval) return false;
            timer.Restart();
            return true;
        }
    }
}
