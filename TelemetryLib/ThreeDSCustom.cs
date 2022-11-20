using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
//using System.Text;
using TelemetryLib.Utilities;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;

namespace TelemetryLib
{
    [ComVisible(true)]
    [Guid("5eb87c33-e05b-4bd0-8128-d62e4abdd3d1")]
    public class ThreeDSCustom : IAnalyticsHost
    {
        private readonly HttpClient client = new HttpClient();
        private TelemetryLogger Logger;
        TelemetryConfiguration Config;

        public ThreeDSCustom(TelemetryLogger logger, TelemetryConfiguration config)
        {
            Logger = logger;
            Config = config;
        }

        public bool ValidateEvent(AnalyticsEvent evt)
        {
            bool isValid = true;

            if (EventNameLengthValidate(evt.EventName))
            {
                evt.addErrorToEvent(evt.EventName, Constants.Event_Name_Length_Invalid);
                evt.Status = EventStatus.ValidationFailed;
                return false;
            }

            if (this.Config.UseEventSchema)
            {
                isValid = CollectionUtilities.eventNameExists(evt.EventName);

                if (!isValid)
                {
                    evt.addErrorToEvent(evt.EventName, Constants.Event_Name_Invalid);
                    evt.Status = EventStatus.ValidationFailed;
                    return false;
                }
            }

            foreach (KeyValuePair<string, object> kvp in evt.getParams())
            {
                if (!this.ValidateEventKeyValue(evt, kvp))
                {
                    isValid = false;
                }
            }

            return isValid;
        }

        // Validate event with respect to Google Analytics v2
        public bool ValidateEvent(AnalyticsEvent evt, Dictionary<string, object> eventparameters)
        {
            bool isValid = true;

            if (EventNameLengthValidate(evt.EventName))
            {
                evt.addErrorToEvent(evt.EventName, Constants.Event_Name_Length_Invalid);
                evt.Status = EventStatus.ValidationFailed;
                return false;
            }

            if (this.Config.UseEventSchema)
            {
                isValid = CollectionUtilities.eventNameExists(evt.EventName);

                if (!isValid)
                {
                    evt.addErrorToEvent(evt.EventName, Constants.Event_Name_Invalid);
                    evt.Status = EventStatus.ValidationFailed;
                    return false;
                }
            }

            foreach (KeyValuePair<string, object> kvp in eventparameters)
            {
                if (this.ValidateEventKeyValue(evt, kvp))
                {
                    evt.add(kvp.Key, kvp.Value);
                }
                else
                {
                    isValid = false;
                }
            }

            return isValid;
        }

        public bool ValidateEventKeyValue(AnalyticsEvent evt, KeyValuePair<string, object> eventparameter)
        {
            bool isValid = true;

            if (EventParamNameLengthValidate(eventparameter))
            {
                evt.addErrorToEvent(eventparameter.Key, Constants.Event_Parameter_Name_Length_Invalid);
                evt.Status = EventStatus.ValidationFailed;
                return false;
            }

            if (EventParamValueLengthValidate(eventparameter))
            {
                evt.addErrorToEvent(eventparameter.Value.ToString(), Constants.Event_Parameter_Value_Length_Invalid);
                evt.Status = EventStatus.ValidationFailed;
                return false;
            }

            if (this.Config.UseEventSchema)
            {
                isValid = CollectionUtilities.eventParamExistsInSchema(evt.EventName, eventparameter.Key);

                if (!isValid)
                {
                    evt.addErrorToEvent(eventparameter.Key, Constants.Event_Parameter_Invalid);
                    evt.Status = EventStatus.ValidationFailed;
                }
            }
            else
            {
                isValid = true;
            }

            return isValid;
        }

        public bool ValidateEventData(AnalyticsEvent evt, string eventParamName, object eventParamValue)
        {
            bool isValid = false;
            if (EventParamNameLengthValidate(eventParamName))
            {
                evt.addErrorToEvent(eventParamName, Constants.Event_Parameter_Name_Length_Invalid);
                evt.Status = EventStatus.ValidationFailed;
                return false;
            }

            if (EventParamValueLengthValidate(eventParamValue.ToString()))
            {
                evt.addErrorToEvent(eventParamValue.ToString(), Constants.Event_Parameter_Value_Length_Invalid);
                evt.Status = EventStatus.ValidationFailed;
                return false;
            }

            if (this.Config.UseEventSchema)
            {
                isValid = CollectionUtilities.eventParamExistsInSchema(evt.EventName, eventParamName);

                if (!isValid)
                {
                    evt.addErrorToEvent(eventParamName, Constants.Event_Parameter_Invalid);
                    evt.Status = EventStatus.ValidationFailed;
                }
            }
            else
            {
                isValid = true;
            }

            return isValid;
        }

        Task IAnalyticsHost.LogEvent()
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, string> PrepareBasicRequestPayload(Dictionary<string, string> postData, string clientID)
        {
            postData.Add("app_id", string.IsNullOrEmpty(this.Config.TrackingID) ? Constants.trackingID.Trim() : this.Config.TrackingID.Trim());
            postData.Add("cid", clientID);
            postData.Add("ip_address", CryptoUtil.EncryptString( CollectionUtilities.GetAvailableIPAddress() ));
            return postData;
        }

        public async Task<bool> SendEventsToTelemetry(string clientID, List<AnalyticsEvent> eventsList)
        {
            HttpResponseMessage responseBody = null;
            bool logged = false;

            try
            {
                Dictionary<string, string> postData = new Dictionary<string, string>();
                this.PrepareBasicRequestPayload(postData, clientID);
                string postRawData = this.PrepareMultiEventRequestPayload(eventsList);

                string queryString = "";
                foreach (KeyValuePair<string, string> kvp in postData)
                {
                    queryString += string.Format("{0}={1}&", Uri.EscapeDataString(kvp.Key), Uri.EscapeDataString(kvp.Value));
                }

                if (queryString.EndsWith("&"))
                {
                    queryString = queryString.Substring(0, queryString.Length - 1);
                }

                var payLoad = new StringContent(postRawData, Encoding.UTF8, "application/json");

                Logger.Information("Telemetry Collection URL  = " + this.Config.CollectionEndPoint + "?" + queryString);
                Logger.Information("Request Payload = " + payLoad.ReadAsStringAsync().Result.ToString());

                responseBody = await client.PostAsync(this.Config.CollectionEndPoint + "?" + queryString, payLoad);
                logged = responseBody.IsSuccessStatusCode;

                Logger.Information("Telemetry Collection API Success = " + responseBody.IsSuccessStatusCode);
                Logger.Information("API Response = " + responseBody.Content.ReadAsStringAsync().Result.ToString());
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught !!!. Error = {0} ", e.Message);
                Logger.Error(e);
            }

            return logged;
        }


        public string PrepareMultiEventRequestPayload(List<AnalyticsEvent> eventLst)
        {
            string rawBodyData = "";
            Payload payload = new Payload();
            payload.events = new List<CustomEvent>();

            try
            {
                foreach (AnalyticsEvent eventObj in eventLst)
                {
                    CustomEvent evnt = new CustomEvent();
                    evnt.name = eventObj.EventName;
                    evnt.session_id = eventObj.SessionId;
                    evnt.user_session_id = eventObj.UserSessionId;
                    evnt.date = eventObj.EventDate;
                    evnt.parameters = new List<Parameter>();

                    foreach (KeyValuePair<string, object> kvp in eventObj.getParams())
                    {
                        Parameter parameter = new Parameter();
                        parameter.name = kvp.Key;
                        parameter.value = kvp.Value;
                        evnt.parameters.Add(parameter);
                    }

                    payload.events.Add(evnt);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nException Caught !!!. Error = {0} ", ex.ToString());
                Logger.Error(ex);
            }

            rawBodyData = JsonConvert.SerializeObject(payload, Formatting.Indented);
            return rawBodyData;
        }

        //---------------------------Private Methods-------------------------------//

        private bool EventNameLengthValidate(string eventName)
        {
            return eventName.Length > 100;
        }

        private bool EventParamNameLengthValidate(KeyValuePair<string, object> eventToValidate)
        {
            return eventToValidate.Key.Length > 100;
        }

        private bool EventParamValueLengthValidate(KeyValuePair<string, object> eventToValidate)
        {
            return (eventToValidate.Value == null || eventToValidate.Value.ToString().Length > 500);
        }

        private bool EventParamNameLengthValidate(string eventKey)
        {
            return eventKey.Length > 100;
        }

        private bool EventParamValueLengthValidate(object eventValue)
        {
            return (eventValue == null || eventValue.ToString().Length > 500);
        }
    }
}