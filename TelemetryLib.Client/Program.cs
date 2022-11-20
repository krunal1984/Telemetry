using System;
using System.Collections.Generic;
using System.Threading;
using TelemetryLib.Utilities;

namespace TelemetryLib.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            TelemetryLogger logger = new TelemetryLogger();
            logger.Information("Telemetry Client Test App Started !!!!!!!!!");

            IAnalyticsManager analyticsManager = AnalyticsFactory.getInstance(null, true);

            logger.Information("Got Instance.");

            if (analyticsManager.GetStatus() == TelemetryStatus.Errored)
            {
                Console.WriteLine("Error initiliasing Telemetry Library");

                foreach (TDSError e in analyticsManager.GetErrors())
                {
                    Console.WriteLine("Error - {{e.Key}} {{e.ErrorMessage}} ");
                }

                Console.WriteLine("Hit any key to close the App !");
                Console.Read();
                Console.WriteLine("App closed");
                System.Environment.Exit(1);
            }
            else
            {
                Console.WriteLine("Telemetry Library initiliased. Internal User = {0} ", analyticsManager.IsInternalUser());
            }

            List<Schema> allSchemas = analyticsManager.getAllSchemas();
            Dictionary<string, object> eventValues = new Dictionary<string, object>();

            logger.Information("Loaded Schemas.");

            //if (allSchemas != null)
            //{
            //    analyticsManager.beginSession();
            //    logger.Information("LOG Events Start Time:" + DateTime.Now.ToString());
            //    int Count = 5;

            //    for (int i = 1; i <= Count; i++)
            //    {
            //        Thread.Sleep(60000);
            //        foreach (Schema schema in allSchemas)
            //        {
            //            foreach (Param param in schema.Params)
            //            {
            //                eventValues.Add(param.Name, param.Default);
            //            }

            //            AnalyticsEvent result;

            //            try
            //            {
            //                logger.Information("LOG Event :" + schema.EventName + ": " + i + " Start Time:" + DateTime.Now.ToString());
            //                result = analyticsManager.logEvent(schema.EventName, eventValues);
            //                logger.Information("Result : " + result);
            //                logger.Information("LOG Event :" + schema.EventName + ": " + i + " End Time:" + DateTime.Now.ToString());
            //            }
            //            catch (Exception ex)
            //            {
            //                logger.Error(ex);
            //            }

            //            eventValues.Clear();
            //        }
            //    }
            //}

            logger.Information("Total Cached Events: " + analyticsManager.getCachedRecordCount());
            logger.Information("System Details:" + analyticsManager.logSystemEnvironmentDetails());

            logger.Information("LOG Test Events Start -");
            AnalyticsEvent resultEvent;

            //Following event is not logged if UseEventSchema is True
            eventValues.Add("TestParam1", "TestValue1");
            eventValues.Add("TestParam100", "TestValue100");
            resultEvent = analyticsManager.logEvent("EventWithouSchema", eventValues);
            Console.WriteLine("Event : " + resultEvent);

            eventValues.Clear();                                            //Clear any old values
            eventValues.Add("Printer_Name", "ProJet MJP 3500");
            eventValues.Add("Printer_Technology", "SLS");
            eventValues.Add("Action", "autoplace_btn_click");
            eventValues.Add("Result", true);
            resultEvent = analyticsManager.logEvent("AutoPlace", eventValues);
            Console.WriteLine("Event : " + resultEvent);

            eventValues.Clear();                                            //Clear any old values
            eventValues.Add("printer_name", "ProJet MJP 5500");
            eventValues.Add("printer_technology", "DMP");
            eventValues.Add("action", "autoplace_btn_click");
            eventValues.Add("result", false);
            resultEvent = analyticsManager.logEvent("AutoPlace", eventValues);
            Console.WriteLine("Event : " + resultEvent);

            eventValues.Clear();
            eventValues.Add("printer_name", "ProJet MJP 5500");
            eventValues.Add("printer_technology", "DMP");
            eventValues.Add("action", "autoplace_btn_click");
            eventValues.Add("no_of_catritiges", 50);
            eventValues.Add("dimensions", 250);
            eventValues.Add("ratio", 234.89);
            eventValues.Add("result", false);
            resultEvent = analyticsManager.logEvent("Material Usage", eventValues);
            Console.WriteLine("Event : " + resultEvent);

            //Following event is not logged if UseEventSchema is True
            AnalyticsEvent e1 = analyticsManager.createEvent("EventWithouSchema2");
            e1.add("TestParam1", "TestValue1");
            e1.add("TestParam100", "TestValue100");
            resultEvent = analyticsManager.logEvent(e1);
            Console.WriteLine("Event : " + resultEvent);

            AnalyticsEvent e2 = analyticsManager.createEvent("File Import");
            e2.add("file_name", "test.obj");
            e2.add("printer_type", "SLA");
            e2.add("file_size", "1256 MB");
            e2.add("number_of_triangles", 400);
            resultEvent = analyticsManager.logEvent(e2);
            Console.WriteLine("Event : " + resultEvent);

            AnalyticsEvent e5 = analyticsManager.createEvent("AutoPlace");
            e5.add("Printer_Name", "ProJet MJP 3500");                    // add event param as Key Value
            e5.add("Printer_Technology", "SLS");
            e5.add("Result", true);
            Console.WriteLine("Event : " + e5);
            resultEvent = analyticsManager.logEvent(e5);
            Console.WriteLine("Event : " + resultEvent);

            logger.Information("LOG Test Events Start -");

            logger.Information("System Details : " + analyticsManager.logSystemEnvironmentDetails(false));

            Console.WriteLine("Hit any key to close the App !");
            Console.Read();
            Console.WriteLine("App closed");
        }
    }
}
