using System;
using Serilog;
using System.IO;
using System.Reflection;

namespace TelemetryLib
{
    public class TelemetryLogger
    {

        public ILogger logger = null;

        public TelemetryLogger(string fileName = "")
        {

            if (string.IsNullOrWhiteSpace(fileName))
            {
                string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                fileName = appPath + "\\TelemetryLog\\TelemetryLog.Txt";
            }

            Console.WriteLine("Initialising Logger at {0}", fileName);

            logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.RollingFile(fileName, fileSizeLimitBytes: 20480000, shared: true)
                .CreateLogger();

            Console.WriteLine("Created a Logger object at ", fileName);
        }

        public void Information(string message)
        {
            logger.Information(message);
        }
        
        public void Error(Exception  ex)
        {
            logger.Error(ex, "Error details - ", ex);            
        }
        
        public void Error(string errorMessage)
        {
            logger.Error("An error occured. Error details - ", errorMessage);
        }
        
    }
}