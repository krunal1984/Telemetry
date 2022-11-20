using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TelemetryLib
{
    public class AnalyticsEvent : IDisposable
    {
        private Dictionary<string, object> parameters = new Dictionary<string, object>();
        private EventStatus status;
        private bool disposed = false;
        internal string EventName;
        internal string EventId = "";
        internal string SessionId = "";
        internal string UserSessionId = "";
        internal bool sendImmediate = false;
        internal DateTime EventDate;
        private List<TDSError> errors = new List<TDSError>();

        public List<TDSError> getErrors()
        {
            return this.errors;
        }

        internal bool IsEventErrored()
        {
            return this.errors.Count > 0;
        }

        public Dictionary<string, object> getParams()
        {
            return this.Parameters;
        }
        
        private Dictionary<string, object> Parameters
        {
            get { return parameters; }
            set { parameters = value; }
        }

        public EventStatus Status
        {
            get { return status; }
            set => status = value;
        }

        private List<TDSError> Errors { get => errors; set => errors = value; }
        
        internal AnalyticsEvent() { }

        internal AnalyticsEvent(string name, string sessionID, string userSessionID)
        {
            this.EventName = name;
            this.EventId = Guid.NewGuid().ToString();
            this.SessionId = sessionID;
            this.UserSessionId = userSessionID;
            this.Status = EventStatus.Initialised;
        }

        internal AnalyticsEvent(string name, Dictionary<string, object> parameters)
        {
            this.EventName = name;
            this.EventId = Guid.NewGuid().ToString();
            this.SessionId = AnalyticsManager.getInstance().sessionID;
            this.UserSessionId = AnalyticsManager.getInstance().userSessionID;
            this.parameters = parameters;
            this.Status = EventStatus.Initialised;
        }

        // Add a single parameter to event
        public bool add(string parameter, object value)
        {
            bool returnValue = false;

            this.Parameters.Add(parameter, value);
            returnValue = true;
            return returnValue;
        }

        // Add a parameter collection to existing collection using Extension method
        public bool add(Dictionary<string, object> parameters)
        {
            bool returnValue = false;
            if (!this.IsEventErrored())
            {
                foreach (KeyValuePair<string, object> kvp in parameters)
                {
                    this.parameters.Add(kvp.Key, kvp.Value);
                }
            }

            return returnValue;
        }

        public override String ToString()
        {
            var json =  JsonConvert.SerializeObject(details());
            return json;
        }

        // Returns dynamic object representing Event data
        public object details()
        {
            var obj = new
            {
                Name = this.EventName,
                EventParameters = this.Parameters,
                EventErrors = this.Errors,
                EventStatus = Enum.GetName(typeof(EventStatus), this.Status)
            };

            return obj;
        }


        public void cancel(bool cancelled = true)
        {
            this.empty();
            if (cancelled is true)
                this.Status = EventStatus.Cancelled;
            else
                this.Status = EventStatus.Disposed;
        }


        public void Dispose()
        {
            dispose(true);
            GC.SuppressFinalize(this);
        }

        public void empty()
        {
            this.EventName = null;
            this.Errors = null;
            this.parameters = null;
            this.SessionId = null;
            this.EventId = null;
          //  this.status = EventStatus.Disposed;

        }

        public void addErrorToEvent(string errorKey, string errorMsg)
        {
            TDSError error = new TDSError();
            error.Key = errorKey;
            error.ErrorMessage = errorMsg;
            this.Errors.Add(error);
        }

        public void addErrorToEvent(List<TDSError> errors)
        {
            foreach(TDSError error in errors)
            {
                this.Errors.Add(error);
                this.Status = EventStatus.Errored;
            }
        }

        // Private method implementation

        protected virtual void dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed resources.
                if (disposing)
                {
                }

                // dispose all un-managed resources here.

                disposed = true;
            }
        }
       
        ~AnalyticsEvent()
        {
            dispose(false);
        }
    }
}

public partial class TDSError
{
    private string key;
    private string errorMessage;

    public string Key { get => key; set => key = value; }
    public string ErrorMessage { get => errorMessage; set => errorMessage = value; }
}
