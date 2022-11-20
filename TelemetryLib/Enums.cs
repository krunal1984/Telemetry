namespace TelemetryLib
{
    public enum AnalyticsSource 
    {
        ThreeDSCustom = 1,
        GoogleAnalyticsV2  = 2
    }

    public enum EventStatus
    {
        Initialised = 1,
        Validated = 2,
        ValidationFailed = 3,
        Cached = 4,
        Logged = 5,
        Errored = 6,
        Cancelled = 7,
        Disposed = 8
        
    }

    public enum TelemetryStatus
    {
        Initialised = 1,
        Validated = 2,
        ValidationFailed = 3,        
        Errored = 4,        

    }

    public enum NetworkType
    {
        IP4Address = 0,
        PublicAddress = 1
    }
}