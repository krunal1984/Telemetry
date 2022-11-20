using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TelemetryClientAPI.Common;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TelemetryClientAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]

    public class TelemetryClientController : Controller
    {        
        TelemetryLib.IAnalyticsManager analyticsManager = null;        
     
        TelemetryClientController()
        {
            analyticsManager = TelemetryLib.AnalyticsFactory.getInstance(Guid.NewGuid().ToString());
        }

        // GET: api/<controller>
        [HttpGet]
        public string Get()
        {
            String res = "test";
            try
            {
                

            }
            catch (Exception ex)
            {
                res = ex.Message.ToLower();
            }
            return Response.StatusCode.ToString();
        }

        // GET api/<controller>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<controller>
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Route("[action]")]
        [HttpPost]
        public string TrackData([FromBody] dynamic input)
        {
            Object json = input;
            JObject jobject = JObject.Parse(json.ToString());
            string res = "";

            try
            {
                string data = jobject.ToString(Newtonsoft.Json.Formatting.None);
                Event eventObj = JsonConvert.DeserializeObject<Event>(data);
                analyticsManager.beginSession();
                Dictionary<string, object> eventValues = new Dictionary<string, object>();
                eventValues.Add("Printer_Name", "ProJet MJP 3500");
                eventValues.Add("Printer_Technology", "SLS");
                eventValues.Add("Action", "autoplace_btn_click");
                eventValues.Add("Result", true);
                res = analyticsManager.logEvent("AutoPlace", eventValues);

                eventValues.Clear();

                if (eventObj.parameters.Count > 0)
                {
                    foreach (Parameter eventParam in eventObj.parameters)
                    {
                        eventValues.Add(eventParam.name, eventParam.value);
                        Console.WriteLine("Event : " + res);
                    }

                }
                res = analyticsManager.logEvent(eventObj.name, eventValues);
            }
            catch (Exception ex)
            {
                res = ex.Message.ToLower();
            }
            return Response.StatusCode.ToString();
        }

        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Route("[action]")]
        [HttpPost]
        public string TrackBatchData([FromBody] dynamic input)

        {
            var json = input;
            JObject jobject = JObject.Parse(json.ToString());
            string res = "";

            try
            {
                //string data = jobject.ToString(Newtonsoft.Json.Formatting.None);
                JToken tracking_data = jobject.GetValue("events");
                List<Event> tracking_data_list = JsonConvert.DeserializeObject<List<Event>>(tracking_data.ToString());
                analyticsManager.beginSession();
                Dictionary<string, object> eventValues = new Dictionary<string, object>();

                foreach (Event eventObj in tracking_data_list)
                {
                    if (eventObj.parameters.Count > 0)
                    {
                        foreach (Parameter eventParam in eventObj.parameters)
                        {
                            eventValues.Add(eventParam.name, eventParam.value);
                            Console.WriteLine("Event : " + res);
                        }

                    }
                    res = analyticsManager.logEvent(eventObj.name, eventValues);
                }
            }
            catch (Exception ex)
            {
                res = ex.Message.ToLower();
            }
            return Response.StatusCode.ToString();
        }

    }
}
