using System;

namespace TelemetryLib
{
    public static class AnalyticsFactory
    {

        public static IAnalyticsManager analyticsManager = null;

        public static IAnalyticsManager getInstance(string clientID = null, bool internalUser = false)
        {
            if ( analyticsManager is null  )
                analyticsManager = AnalyticsManager.getInstance(clientID, internalUser);

            return analyticsManager;
        }
    }
}