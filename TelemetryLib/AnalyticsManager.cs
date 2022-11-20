using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TelemetryLib.Scheduler;
using TelemetryLib.Utilities;

namespace TelemetryLib
{

    public class AnalyticsManager : IAnalyticsManager
    {
        internal string sessionID = "";
        internal string userSessionID = "";
        private string clientID = "";
        internal List<Schema> schemas = null;
        private static AnalyticsManager analyticsManager = null;
        internal DBContext dbContext = null;
        internal TelemetryConfiguration config = null;
        internal IAnalyticsHost analyticsHost;
        internal TelemetryLogger Logger = new TelemetryLogger();

        private TelemetryStatus status = TelemetryStatus.Initialised;
        private List<TDSError> errors = new List<TDSError>();
        private bool internalUser = false;

        public AnalyticsManager(string clientId, bool internalUser)
        {
            try
            {
                Console.WriteLine("Loading configuration !!!");

                this.config = CollectionUtilities.ReadConfig();

                Console.WriteLine("Configuration for " + this.config.ApplicationName + " loaded");
                Logger.Information("Configuration for " + this.config.ApplicationName + " loaded");

                if (this.config.UseEventSchema == true)
                {
                    this.schemas = CollectionUtilities.ReadSchema();
                }

                this.clientID = (String.IsNullOrWhiteSpace(clientId) ? GetNewClientId() : clientId);
                this.sessionID = Guid.NewGuid().ToString();
                this.internalUser = internalUser;

                analyticsHost = CollectionUtilities.GetPlatform(this.config, Logger);
                dbContext = new DBContext();
                EventScheduler.Start(this.config).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                addErrorToTelemetry("Error", ex.ToString());
                status = TelemetryStatus.Errored;
                Logger.Error(ex);
            }
        }

        internal static AnalyticsManager getInstance()
        {
            return getInstance(null, false);
        }

        public static AnalyticsManager getInstance(string clientID, bool internalUser = false)
        {
            Console.WriteLine("AnalyticsManager istance is set = {0}", analyticsManager == null);

            if (analyticsManager == null)
            {
                try
                {
                    analyticsManager = new AnalyticsManager(clientID, internalUser);
                    analyticsManager.logSystemEnvironmentDetails(true, "System Environments");
                    CollectionUtilities.UpdatePublicIPInDB();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            return analyticsManager;
        }

        public TelemetryStatus GetStatus()
        {
            return status;
        }


        public List<TDSError> GetErrors()
        {
            return errors;
        }

        public bool IsInternalUser()
        {
            return internalUser;
        }


        private string GetNewClientId()
        {
            return Guid.NewGuid().ToString().Trim(); //Environment.MachineName;
        }

        internal TelemetryConfiguration getConfiguration()
        {
            return this.config;
        }

        public List<Schema> getAllSchemas()
        {
            return this.schemas;
        }


        public String logSystemEnvironmentDetails(bool logEvent = false, string eventName="System Details")
        {
            //return JsonConvert.SerializeObject(CollectionUtilities.getSystemEnvironments(), Formatting.Indented);

            Dictionary<string, object> systemDetails = CollectionUtilities.getSystemEnvironments();
            if( eventName.CompareTo("System Environments") != 0 && eventName.CompareTo("System Details") != 0 ) {
                eventName = $@"SU_{eventName}";
            }
            AnalyticsEvent evt = new AnalyticsEvent(eventName, systemDetails);
            //    AnalyticsManager.getInstance().logEvent(evt);

            if (logEvent == true)
            {
                Task.Run(async () =>
                {
                    this.logEvent(evt);
                });
            }

            string details = JsonConvert.SerializeObject(evt, Formatting.Indented);
            return details;
        }


        public void beginSession()
        {
            this.userSessionID = Guid.NewGuid().ToString();
        }

        public void completeSession()
        {
            this.userSessionID = "";
        }

        public void endSession()
        {
            this.userSessionID = "";
        }

        public string getCachedRecordCount()
        {
            string cachedCount = string.Empty;
            try
            {
                using (var dbContext = new DBContext())
                {
                    cachedCount = dbContext.Events.ToList().Count().ToString();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return cachedCount;
        }


        public AnalyticsEvent logEvent(AnalyticsEvent evnt)
        {
            try
            {
                if (!evnt.IsEventErrored())                             //If AnalyticsManager object is in Error State
                {
                    if (analyticsHost.ValidateEvent(evnt))
                    {
                        if (evnt.IsEventErrored())
                        {
                            return evnt;
                        }

                        Task.Run(async () =>
                        {
                            //await logToEndPoint(evnt);
                            await CollectionUtilities.saveToCache(evnt, true);
                            evnt.Status = EventStatus.Cached;
                        });
                    }
                }
                else
                {
                    Console.WriteLine("Event errored !!!");
                }
            }
            catch (Exception ex)
            {
                evnt.Status = EventStatus.Errored;
                Logger.Error(ex);
            }

            return evnt;
        }


        public AnalyticsEvent logEvent(string eventName, Dictionary<string, object> eventParameters)
        {
            AnalyticsEvent evntObj = new AnalyticsEvent(eventName, this.sessionID, this.userSessionID);

            if (IsErrored())                    //If AnalyticsManager object is in Error State
            {
                evntObj.addErrorToEvent(this.errors);
                return evntObj;
            }

            try
            {
                if (analyticsHost.ValidateEvent(evntObj, eventParameters))
                {
                    if (evntObj.IsEventErrored())
                    {
                        return evntObj;
                    }

                    Task.Run(async() =>{
                        await CollectionUtilities.saveToCache(evntObj, true);
                        evntObj.Status = EventStatus.Cached;
                    });
                }
            }
            catch (Exception ex)
            {
                evntObj.Status = EventStatus.Errored;
                Logger.Error(ex);
            }

            //Console.WriteLine("Shutting down Scheduler !!!!!!!!!!!!!!!!!1");
            //EventScheduler.Stop();              //For Testing

            return evntObj;
        }

        // ---------------------------- Create Event and Log Event Methods Start ---------------------------------------

        public AnalyticsEvent createEvent(string name, Dictionary<string, object> parameters = null)
        {
            AnalyticsEvent evt = new AnalyticsEvent(name, this.sessionID, this.userSessionID);

            if (IsErrored())                            //If AnalyticsManager object is in Error State
            {
                evt.addErrorToEvent(this.errors);
                return evt;
            }

            if (analyticsHost.ValidateEvent(evt))
            {
                if (parameters != null && parameters.Count > 0)
                {
                    foreach (KeyValuePair<string, object> kvp in parameters)
                    {
                        if (!analyticsHost.ValidateEventKeyValue(evt, kvp))
                        {
                            evt.addErrorToEvent(name, Constants.Event_Parameter_Invalid);
                            evt.Status = EventStatus.ValidationFailed;
                        }
                        else
                        {
                            evt.add(kvp.Key, kvp.Value);
                        }
                    }
                }

                evt.Status = EventStatus.Initialised;
            }

            return evt;
        }

        // ---------------------------- Create Event and Log Event Methods End ---------------------------------------

        // ------------------ private functions ----------------------- //
                      

     
        internal bool IsErrored()
        {
            return this.status == TelemetryStatus.Errored && this.errors.Count > 0;
        }
        
        internal async Task<bool> sendCachedEventToEndPoint(List<AnalyticsEvent> eventsList)
        {
            bool result = await analyticsHost.SendEventsToTelemetry(this.clientID, eventsList);

            return result;
        }


        public void addErrorToTelemetry(string errorKey, string errorMsg)
        {
            TDSError error = new TDSError();
            error.Key = errorKey;
            error.ErrorMessage = errorMsg;
            this.errors.Add(error);
        }
    }
}