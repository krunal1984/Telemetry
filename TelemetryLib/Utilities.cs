using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using Newtonsoft.Json;
using TelemetryLib.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Management;
using System.Reflection;
using Quartz.Logging;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace TelemetryLib.Utilities
{
    public static class CollectionUtilities
    {

        public const string Network_File_Path = "network-data.json";

        // Add collection to main via an Extension method
        public static void AddDictionary<T, S>(this Dictionary<T, S> source, Dictionary<T, S> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("Collection is null");
            }

            foreach (var item in collection)
            {
                if (!source.ContainsKey(item.Key))
                {
                    source.Add(item.Key, item.Value);
                }
                else
                {
                    // handle duplicate keys
                }
            }
        }

        // Validate event via an Extension method
        public static List<string> Validate(this AnalyticsEvent source, List<Schema> allSchemaDef)
        {
            List<string> errors = new List<string>();

            //Validate event parameters with corresponding schema defination

            return errors;
        }

        public static TelemetryConfiguration ReadConfig()
        {
            TelemetryConfiguration config = new TelemetryConfiguration();
            Serializer ser = new Serializer();
            string xmlInputData = string.Empty;

            //path = Directory.GetCurrentDirectory() + @"\telemetry.config.json";
            //Assembly currentAssem = Assembly.GetExecutingAssembly();
            //string assemblyPath = Assembly.GetExecutingAssembly().Location;

            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\telemetry.config.json";

            if (File.Exists(assemblyPath))
            {
                xmlInputData = File.ReadAllText(assemblyPath);
                config = ser.DeserializeFromJSON<TelemetryConfiguration>(xmlInputData);
            }
            else
            {
                throw new FileNotFoundException("Configuration file " + assemblyPath + " not found.");
            }

            return config;
        }

        public static List<Schema> ReadSchema()
        {
            List<Schema> allSchemas = new List<Schema>();
            Serializer ser = new Serializer();
            string path = string.Empty;
            string xmlInputData = string.Empty;

            path = Directory.GetCurrentDirectory() + @"\Schema";
            if (Directory.Exists(path))
            {
                string[] fileEntries = Directory.GetFiles(path);

                foreach (string fileName in fileEntries)
                {
                    if ( fileName.ToUpper().EndsWith("SCHEMA.JSON"))
                    {
                        xmlInputData = File.ReadAllText(fileName);
                        Schema schema = ser.DeserializeFromJSON<Schema>(xmlInputData);
                        allSchemas.Add(schema);
                    }
                }
            }

            return allSchemas;
        }


        internal static bool sendImmediate(string eventName)
        {
            bool sendImmediate = false;
            
            try
            {
                if (AnalyticsManager.getInstance().schemas != null)
                {
                    Schema schema = AnalyticsManager.getInstance().schemas.Where(x => x.EventName == eventName).DefaultIfEmpty().First();

                    if (schema != null)
                    {
                        sendImmediate = schema.ImmidiateSend;
                    }
                    else
                    {
                        sendImmediate = AnalyticsManager.getInstance().getConfiguration().ImmidiateSend;
                    }
                }
            }
            catch (Exception ex)
            {
                AnalyticsManager.getInstance().Logger.Error(ex);
            }

            return sendImmediate;
        }

        public static bool eventNameExists(string eventName)
        {

            bool evtNameExists = false;
            if (!string.IsNullOrEmpty(eventName))
            {
                evtNameExists = AnalyticsManager.getInstance().schemas.Where(x => x.EventName == eventName).Any();
            }
            return evtNameExists;
        }

        public static bool eventParamExistsInSchema(string eventName, string paramName)
        {
            bool evtParamExists = false;

            if (!string.IsNullOrEmpty(eventName))
            {
                try
                {
                    if (AnalyticsManager.getInstance().schemas != null)
                    {
                        Schema schema = AnalyticsManager.getInstance().schemas.Where(x => x.EventName == eventName).First();

                        evtParamExists = schema.Params.Where(x => x.Name == paramName).Any();
                    }
                }
                catch (Exception ex)
                {
                    AnalyticsManager.getInstance().Logger.Error(ex);
                }
            }

            return evtParamExists;
        }

        public static async Task saveToCache(AnalyticsEvent eventObj, bool sendImmediate = false)
        {
            try
            {
                using (var dbContext = new DBContext())
                {
                    dbContext.Events.Add(new Models.Event { EventName = eventObj.EventName, EventParams = JsonConvert.SerializeObject(eventObj.getParams()), SessionId = eventObj.SessionId, UserSessionId = eventObj.UserSessionId, EventDate = DateTime.UtcNow, sendImmediate = sendImmediate });
                    await dbContext.SaveChangesAsync();
                    eventObj.empty();
                    eventObj.Dispose();
                }
            }
            catch (Exception ex)
            {
                AnalyticsManager.getInstance().Logger.Error(ex);
            }

        }

        internal static async Task sendEventBatch()
        {
            List<Event> events = await CollectionUtilities.getEventBatchFromDB();

            try
            {
                int batchSize = 50;

                if (events.Count > 50)
                    batchSize = (int) events.Count / 5;

                if (events != null && events.Count > 0)
                {
                    for (int i = 0; i < events.Count; i = i + batchSize)
                    {
                        List<Event> batchEvents = events.Skip(i).Take(batchSize).ToList();

                        bool result = await CollectionUtilities.sendEventBatch(batchEvents);

                        if (result)
                        {
                            await CollectionUtilities.removeEventsFromDB(batchEvents);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AnalyticsManager.getInstance().Logger.Error(ex);
            }
        }

        private static async Task <List<Event>> getEventBatchFromDB()
        {
            List<Event> events = null;

            using (var dbContext = new DBContext())
            {
                events = await dbContext.Events.Take(500).ToListAsync();
                AnalyticsManager.getInstance().Logger.Information("Cached Events pulled by Job : " + events.Count);
            }

            return events;
        }

        private static async Task<bool> sendEventBatch(List<Event> batchEvents)
        {
            bool result = false;

            List<AnalyticsEvent> batchEventsLst = new List<AnalyticsEvent>();

            foreach (Event evnt in batchEvents)
            {
                try
                {
                    AnalyticsEvent evntObj = new AnalyticsEvent();

                    evntObj.SessionId = evnt.SessionId;
                    evntObj.UserSessionId = evnt.UserSessionId;
                    evntObj.EventName = evnt.EventName;
                    evntObj.EventDate = evnt.EventDate;
                    evntObj.add(JsonConvert.DeserializeObject<Dictionary<string, object>>(evnt.EventParams));

                    batchEventsLst.Add(evntObj);
                }
                catch (Exception ex)
                {
                    AnalyticsManager.getInstance().Logger.Error(ex);
                }
            }

            result = await AnalyticsManager.getInstance().sendCachedEventToEndPoint(batchEventsLst);

            return result;
        }

        private static async Task removeEventsFromDB(List<Event> batchEvents)
        {
            using (var dbContext = new DBContext())
            {
                dbContext.Events.RemoveRange(batchEvents);
                await dbContext.SaveChangesAsync();
            }
        }
       
        internal static Dictionary<string, object> getSystemEnvironments()
        {
            Dictionary<string, object> systemDetails = new Dictionary<string, object>();
            systemDetails.Add("Machine Name", Environment.MachineName);
            systemDetails.Add("Application Name", AnalyticsManager.getInstance().config.ApplicationName);
            systemDetails.Add("Is 64 bit processor", Environment.Is64BitProcess);
            systemDetails.Add("Is 64 bit OS", Environment.Is64BitOperatingSystem);
            systemDetails.Add("CPU Usage", GetCpuUsage() + " %");
            systemDetails.Add("GPU Usage", GetGPUUsage());
            systemDetails.Add("Memory Usage", GetMemoryUsage());
           // systemDetails.Add("Language", Environment.language);

            var wmi = new ManagementObjectSearcher("select * from Win32_OperatingSystem").Get().Cast<ManagementObject>().First();
            OS operatingsystem = new OS();
            systemDetails.Add("Operating System", ((string)wmi["Caption"]).Trim());
            systemDetails.Add("Operating system version", (string)wmi["Version"]);
            operatingsystem.TotalPhysicalMemory = getBytes(((UInt64)wmi["TotalVisibleMemorySize"]) / 1000) + " GB";
            operatingsystem.FreePhysicalMemory = getBytes(((UInt64)wmi["FreePhysicalMemory"]) / 1000) + " GB";
            operatingsystem.FreeVirtualMemory = getBytes(((UInt64)wmi["FreeVirtualMemory"]) / 1000) + " GB";
            operatingsystem.AvailablePhysicalMemory = getBytes(((UInt64)wmi["TotalVisibleMemorySize"]) / 1000) - getBytes(((UInt64)wmi["FreePhysicalMemory"]) / 1000) + " GB";

            var csi = new ManagementObjectSearcher("select * from Win32_ComputerSystem").Get().Cast<ManagementObject>().First();
            systemDetails.Add("System manufacturer", ((string)csi["Manufacturer"]).Trim());
            systemDetails.Add("System model", (string)csi["Model"]);


            var cpu = new ManagementObjectSearcher("select * from Win32_Processor").Get().Cast<ManagementObject>().First();
            CPUDetails cpudetails = new CPUDetails();
            cpudetails.CPU = (string)cpu["Name"];
            cpudetails.CPUArchitecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
            cpudetails.NoofCores = (uint)cpu["NumberOfCores"];
            cpudetails.NoofThreads = (uint)cpu["NumberOfLogicalProcessors"];
            Dictionary<string, object> operatingSystem = new Dictionary<string, object>();
            Dictionary<string, object> cpuDetails = new Dictionary<string, object>();
            Dictionary<string, object> videoControlDetails = new Dictionary<string, object>();

            var videoControl = new ManagementObjectSearcher("select * from Win32_VideoController").Get().Cast<ManagementObject>().First();

            VideoControl vcObj = new VideoControl();
            vcObj.GraphicsName = (string)videoControl["Name"];
            uint ram = (uint)videoControl["AdapterRAM"];
            vcObj.AdapterRAM = (double)(ram / 1048576);
            vcObj.AdapterDACType = (string)videoControl["AdapterDACType"];
            vcObj.DriverVersion = (string)videoControl["DriverVersion"];
            vcObj.VideoProcessor = (string)videoControl["VideoProcessor"];

            operatingSystem = operatingsystem.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
               .ToDictionary(prop => prop.Name, prop => prop.GetValue(operatingsystem, null));

            cpuDetails = cpudetails.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(prop => prop.Name, prop => prop.GetValue(cpudetails, null));

            videoControlDetails = vcObj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(prop => prop.Name, prop => prop.GetValue(vcObj, null));

            AddRange(operatingSystem, systemDetails);
            AddRange(cpuDetails, systemDetails);
            AddRange(videoControlDetails, systemDetails);

            return systemDetails;
        }

        internal static Dictionary<string, object> getSystemSnapshot()
        {
            Dictionary<string, object> systemSnapshot = new Dictionary<string, object>();
            systemSnapshot.Add("CPU Usage", GetCpuUsage() + " %");
            systemSnapshot.Add("GPU Usage", GetGPUUsage());
            systemSnapshot.Add("Memory Usage", GetMemoryUsage());
            var wmi = new ManagementObjectSearcher("select * from Win32_OperatingSystem").Get().Cast<ManagementObject>().First();
            OS operatingsystem = new OS();       
            operatingsystem.TotalPhysicalMemory = getBytes(((UInt64)wmi["TotalVisibleMemorySize"]) / 1000) + " GB";
            operatingsystem.FreePhysicalMemory = getBytes(((UInt64)wmi["FreePhysicalMemory"]) / 1000) + " GB";
            operatingsystem.FreeVirtualMemory = getBytes(((UInt64)wmi["FreeVirtualMemory"]) / 1000) + " GB";
            operatingsystem.AvailablePhysicalMemory = getBytes(((UInt64)wmi["TotalVisibleMemorySize"]) / 1000) - getBytes(((UInt64)wmi["FreePhysicalMemory"]) / 1000) + " GB";
            Dictionary<string, object> operatingSystem = new Dictionary<string, object>();         
            operatingSystem = operatingsystem.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
               .ToDictionary(prop => prop.Name, prop => prop.GetValue(operatingsystem, null));        
     
            AddRange(operatingSystem, systemSnapshot);                

            return systemSnapshot;
        }

        private static double getBytes(UInt64 memoryValue)
        {

            return Math.Round(Convert.ToDouble(memoryValue) / 1024, 0);

        }

        public static int GetCpuUsage()
        {
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", Environment.MachineName);
            cpuCounter.NextValue();
            System.Threading.Thread.Sleep(1000); //This avoid that answer always 0
            return (int)cpuCounter.NextValue();
        }

        public static float GetGPUUsage() {
            float result = 0f;
            List<PerformanceCounter> gpuCounters = GetGPUCounters();
            gpuCounters.ForEach( x => _ = x.NextValue());
            System.Threading.Thread.Sleep(1000);
            gpuCounters.ForEach( x => result += x.NextValue());
            return result;
        }

        public static List<PerformanceCounter> GetGPUCounters() {
            var list = new List<PerformanceCounter>();
            try{
                var category = new PerformanceCounterCategory("GPU Engine");
                var names = category.GetInstanceNames();
                foreach (var name in names){
                    foreach( PerformanceCounter counter in category.GetCounters(name)) {
                        if( counter.CounterName == "Utilization Percentage") {
                            list.Add(counter);
                        }
                    }
                }
            } catch( Exception ex) {
                AnalyticsManager.getInstance().Logger.Error(ex);
            }
            return list;
        }

        public static double GetMemoryUsage() {
            double memoryUsage = 0;
            var wmi = new ManagementObjectSearcher("select * from Win32_OperatingSystem").Get().Cast<ManagementObject>().First();
            double totalMemorySize = getBytes(((UInt64)wmi["TotalVisibleMemorySize"]) / 1000);
            double freeMemorySize = getBytes(((UInt64)wmi["FreePhysicalMemory"]) / 1000);
            memoryUsage = freeMemorySize / totalMemorySize * 100;
            return memoryUsage;
        }

        public static Dictionary<T, S> AddRange<T, S>(Dictionary<T, S> collection, Dictionary<T, S> source)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("Collection is null");
            }

            foreach (var item in collection)
            {
                if (!source.ContainsKey(item.Key))
                {
                    source.Add(item.Key, item.Value);
                }
                else
                {
                    // handle duplicate key issue here
                }
            }
            return source;
        }

        public static IAnalyticsHost GetPlatform(TelemetryConfiguration config, TelemetryLogger logger)
        {
            IAnalyticsHost returnValue = null;
            AnalyticsSource analyticsSource = AnalyticsSource.ThreeDSCustom;

            Console.WriteLine("Configuration = " + config);

            if (config.EndPointType.ToString().Trim().ToUpper() == "ThreeDSCustom".ToUpper())
            {
                analyticsSource = AnalyticsSource.ThreeDSCustom;
                returnValue = new ThreeDSCustom(logger, config);
            }
            else if (config.EndPointType.ToString().Trim().ToUpper() == "GoogleAnalyticsV2".ToUpper())
            {
                analyticsSource = AnalyticsSource.GoogleAnalyticsV2;
            }

            if (analyticsSource != AnalyticsSource.ThreeDSCustom )
                throw new Exception("Only 3DS Custom Analytics is supported.");

            return returnValue;
        }

        public static string GetIPAddress()
        {
            string ip = "";

            try
            {
                WebRequest request = WebRequest.Create(AnalyticsManager.getInstance().config.IPProvider);

                using (WebResponse response = request.GetResponse())

                using (StreamReader stream = new StreamReader(response.GetResponseStream()))
                {
                    ip = stream.ReadToEnd();
                }

            }
            catch (WebException wex)
            {
                ip = "";
                AnalyticsManager.getInstance().Logger.Error(wex);
            }
            catch (Exception ex)
            {
                ip = "Error";
                AnalyticsManager.getInstance().Logger.Error(ex);
            }

            return ip;
        }

        public static Network loadNetworkFile() {
            Network network = null;
            try{
                if( !File.Exists(Network_File_Path) ) return network;
                using( StreamReader reader = new StreamReader(Network_File_Path)) {
                    string json = reader.ReadToEnd();
                    network = JsonConvert.DeserializeObject<Network>(json);
                }
            } catch( Exception ex ) {
                AnalyticsManager.getInstance().Logger.Error(ex);
            }
            return network;
        }

        public static void saveNetworkFile(Network network) {
            File.WriteAllText(Network_File_Path, JsonConvert.SerializeObject(network));
        }

        public static string UpdatePublicIPInDB( ) {
            NetworkType type = NetworkType.PublicAddress;
            string ipAddress = "";
            try{
                
                ipAddress = CollectionUtilities.GetIPAddress();
                var network = loadNetworkFile();

                if( IsIpValid(ipAddress) ) {
                    saveNetworkFile( new Network{
                        Type = (int)type,
                        Address = ipAddress
                    });
                } else {
                    if( network != null && File.Exists(Network_File_Path) ) {

                        File.Delete(Network_File_Path);
                    }
                }

            }catch(Exception ex) {
                AnalyticsManager.getInstance().Logger.Error(ex);
            }
            return ipAddress;
        }

        public static string GetIPAddressFromDBOrServer( NetworkType type) {
            string ipAddress = "";
            try{
                switch( type ) {
                    case NetworkType.IP4Address: {
                        ipAddress = GetLocalIPAddress();
                        break;
                    }
                    case NetworkType.PublicAddress: {
                            Network network = loadNetworkFile();
                            ipAddress = network.Address;
                            if( ipAddress == null ) {
                                ipAddress = UpdatePublicIPInDB();
                            }
                        break;
                    }
                }
            } catch ( Exception ex ) {
                AnalyticsManager.getInstance().Logger.Error(ex);
            }
            return ipAddress;
        }

        public static string GetAvailableIPAddress() {
            string ipAddress = "";
            ipAddress = GetIPAddressFromDBOrServer( NetworkType.PublicAddress );
            if( !IsIpValid(ipAddress) ) {
                ipAddress = GetIPAddressFromDBOrServer( NetworkType.IP4Address );
            }
            return ipAddress;
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "";
        }

        public static bool IsIpValid( string ip ) {
            string ipAddressRegexPattern = @"^([1-9]|[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])(\.([0-9]|[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5])){3}$";   
            Regex regex = new Regex( ipAddressRegexPattern );
            
            return regex.IsMatch( ip );
        }

    }

    public static class CryptoUtil {
        private static readonly byte[] key1 = Encoding.UTF8.GetBytes("CB933AF2BF1271F6A4D114540A3B63E0");
        private static readonly byte[] iV1 = Encoding.UTF8.GetBytes("A5ECDBF17EE02178");

        private static string Encrypt(string text, byte[] key, byte[] iV)
        {
            using (var aesAlg = Aes.Create())
            {
                using (var encryptor = aesAlg.CreateEncryptor(key, iV))
                {
                    using (var msEncrypt = new MemoryStream())
                    {
                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(text);
                        }

                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
        }

        public static string EncryptString( string msg ) {
            string encryptedMsg = Encrypt( msg, key1, iV1);
            return encryptedMsg;
        }

        public static string DecryptString( string msg ) {
            string decryptedMsg =  Decrypt( msg, key1, iV1);
            return decryptedMsg;
        }
        
        private static string Decrypt(string cipherText, byte[] key, byte[] iV)
        {
            var fullCipher = Convert.FromBase64String(cipherText);

            using (var aesAlg = Aes.Create())
            {
                using (var decryptor = aesAlg.CreateDecryptor(key, iV))
                {
                    string result;
                    using (var msDecrypt = new MemoryStream(fullCipher))
                    {
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (var srDecrypt = new StreamReader(csDecrypt))
                            {
                                result = srDecrypt.ReadToEnd();
                            }
                        }
                    }

                    return result;
                }
            }
        }
    }

    internal class OS
    {
        public string FreePhysicalMemory { get; internal set; }
        public string FreeVirtualMemory { get; internal set; }
        public string TotalPhysicalMemory { get; internal set; }

        public string AvailablePhysicalMemory { get; internal set; }
    }
    internal class CPUDetails
    {
        //    public string Socket { get; internal set; }
        public string CPU { get; internal set; }
        public uint NoofThreads { get; internal set; }
        public uint NoofCores { get; internal set; }
        public string CPUArchitecture { get; internal set; }


    }


    internal class VideoControl
    {
        public string GraphicsName { get; internal set; }
        public double AdapterRAM { get; internal set; }
        public string AdapterDACType { get; internal set; }
        public string DriverVersion { get; internal set; }
        public string VideoProcessor { get; internal set; }
    }

    internal class Errors
    {
        private string key;
        private string errorMessage;

        public string Key { get => key; set => key = value; }
        public string ErrorMessage { get => errorMessage; set => errorMessage = value; }
    }

    public class Serializer
    {

        public T DeserializeFromJSON<T>(string input) where T : class
        {
            return JsonConvert.DeserializeObject<T>(input);
        }


        public T Deserialize<T>(string input) where T : class
        {
            System.Xml.Serialization.XmlSerializer ser = new XmlSerializer(typeof(T));

            using (StringReader sr = new StringReader(input))
            {
                return (T)ser.Deserialize(sr);
            }
        }

        public string Serialize<T>(T ObjectToSerialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(ObjectToSerialize.GetType());

            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, ObjectToSerialize);
                return textWriter.ToString();
            }
        }
    }

    public class MemoryMetrics
    {
        public double TotalPhysicalMemory;
        public double UsedMemorySize;
        public double FreePhysicalMemory;
    }

    public class MemoryMetricsClient
    {
        public MemoryMetrics GetMetrics()
        {
            return GetWindowsMetrics();
        }

        private MemoryMetrics GetWindowsMetrics()
        {
            var output = "";
            var info = new ProcessStartInfo();
            info.FileName = "wmic";
            info.Arguments = "OS get FreePhysicalMemory,TotalVisibleMemorySize /Value";
            info.RedirectStandardOutput = true;
            info.WindowStyle = ProcessWindowStyle.Hidden;
            using (var process = Process.Start(info))
            {
                output = process.StandardOutput.ReadToEnd();
            }
            var lines = output.Trim().Split("\n".ToCharArray());
            var freeMemoryParts = lines[0].Split("=".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var totalMemoryParts = lines[1].Split("=".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var metrics = new MemoryMetrics();
            metrics.TotalPhysicalMemory = Math.Round(double.Parse(totalMemoryParts[1]) / 1024, 0);
            metrics.FreePhysicalMemory = Math.Round(double.Parse(freeMemoryParts[1]) / 1024, 0);
            metrics.UsedMemorySize = metrics.TotalPhysicalMemory - metrics.FreePhysicalMemory;
            return metrics;
        }

    }

    public class Parameter
    {
        public string name { get; set; }
        public object value { get; set; }
    }

    public class CustomEvent
    {
        public string name { get; set; }
        public string session_id { get; set; }
        public string user_session_id { get; set; }

        public DateTime date { get; set; }
        public IList<Parameter> parameters { get; set; }
    }

    public class Payload
    {
        public IList<CustomEvent> events { get; set; }
    }
}

