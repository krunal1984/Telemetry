
using System;
using System.Xml.Serialization;
using Newtonsoft.Json;


namespace TelemetryLib
{


    [Serializable, XmlRoot("user")]
    public class TelemetryConfiguration
    {
        public TelemetryConfiguration() {}          //Needed for Serialisation

        public string TrackingID; 
        public string ApplicationName;
        public bool UseEventSchema;
        public bool ImmidiateSend;
        public string Platform;
        public string ScheduleCachedhedEventsTime;
        public string EndPointType;
        public string CollectionEndPoint;
        public string IPProvider;

        public override string ToString()
        {
            var json = JsonConvert.SerializeObject(this);
            return json;
        }
    }
}