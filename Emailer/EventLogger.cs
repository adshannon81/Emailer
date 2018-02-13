using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emailer
{
    internal class EventLogger
    {
        private EventLog _eventLog;
        private string _logName;
        private string _logSource;

        public EventLogger(string logName, string logSource)
        {
            _eventLog = new EventLog();
            _logName = logName;
            _logSource = logSource;
            CheckLogExists();

        }

        public bool WriteEntry(string message, EventLogEntryType eventType)
        {
            _eventLog = new EventLog();
            try
            {
                CheckLogExists();
                _eventLog.WriteEntry(message, eventType);
            }
            catch
            {
                return false;
            }
            return true;
        }

        private void CheckLogExists()
        {
            if (!System.Diagnostics.EventLog.SourceExists(_logSource))
            {
                System.Diagnostics.EventLog.CreateEventSource(_logSource, _logName);
            }
            _eventLog.Source = _logSource;
            _eventLog.Log = _logName;
        }
    }
}
