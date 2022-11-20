using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace TelemetryLib
{
    /*
    [ComVisible(true)]
    [Guid("2ee70a51-168c-44fe-b6e8-b3000f3a9796")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            */
    public interface IAnalyticsManager
    {
        TelemetryStatus GetStatus();

        List<TDSError> GetErrors();

        bool IsInternalUser();

        void beginSession ();

        void completeSession ();
        
        void endSession ();

        AnalyticsEvent logEvent (string eventName, Dictionary<string, object> eventparameters);

        AnalyticsEvent logEvent(AnalyticsEvent evt);

        AnalyticsEvent createEvent  (string eventName, Dictionary<string, object> eventparameters = null);

        string getCachedRecordCount();

        string logSystemEnvironmentDetails(bool logEvent = false, string eventName="System Details");

        List<Schema> getAllSchemas();
    }
}