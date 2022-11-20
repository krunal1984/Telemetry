using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace TelemetryLib
{
    [ComVisible(true)]
    [Guid("3b3ff5b3-2f26-4221-96ec-522cf611cc70")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAnalyticsHost
    {
        // validate data with respect to Analytics Host i.e. GA v1 may have diff. validations compared to GA v2.
        bool ValidateEvent(AnalyticsEvent evt);

        bool ValidateEvent(AnalyticsEvent evt, Dictionary<string, object> eventparameters);

        bool ValidateEventKeyValue(AnalyticsEvent evt, KeyValuePair<string, object> eventparameter);

        bool ValidateEventData(AnalyticsEvent evt, string eventParamName, object eventParamValue);

        Task  LogEvent();

        Task<bool> SendEventsToTelemetry(string clientID, List<AnalyticsEvent> eventsList);
    }
}