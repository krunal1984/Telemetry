using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace TelemetryLib
{
        /*
    [ComVisible(true)]
    [Guid("d04ce87b-29e1-4437-8c1c-d363b1390cd8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        */
    public interface IAnalytics
    {
        void startBatch();

        Task completeBatch();

        void abandonBatch();

        void addTrackingData(string category, string action, double value, string location="");

        void addCompexTrackingData(TrackingContext track);

        Task<string> pushTrackingData(string category, string action, double value, string location="");

        Task<string> pushCompexTrackingData(TrackingContext track);


        void startSession(string sessionID = "");

        void completeSession();

    }
}