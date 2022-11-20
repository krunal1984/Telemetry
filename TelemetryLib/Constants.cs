namespace TelemetryLib
{
    internal class Constants
    {
        public const string version = "2";

        // public const string url = "http://10.249.0.46/api/telemetry/collect"; //"http://localhost:8000/api/telemetry/collect"; "http://10.70.1.44/api/telemetry/collect";
        // public const string url = "http://localhost:8000/api/telemetry/collect"; 
        
        public const string trackingID = "3DSprint";
       
        public const string Event_Name_Invalid = "Event Name not found in schema";

        public const string Event_Name_Length_Invalid = "Event Name must not exceed more than 40 characters";

        public const string Event_Parameter_Invalid = "Event Parameter not found in schema";

        public const string Event_Parameter_Name_Length_Invalid = "Event Parameter Name must not exceed more than 40 characters";

        public const string Event_Parameter_Value_Length_Invalid = "Event Parameter Value must not exceed more than 100 characters";


        public const string Configuration_not_found = "Configuration file not found in Application, add one to send data to Telemetry";

        public const string Schema_folder_not_found = "Schema folder was not found in Application, add all events schemas to the Schema folder";

        public const string Event_Schema_not_found = "Event Schema not found in Schema folder, add one to send data to Telemetry";
    }
}