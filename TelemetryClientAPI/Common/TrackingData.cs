using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TelemetryClientAPI.Common
{
    public class Parameter
    {
        public string name { get; set; }
        public object value { get; set; }
    }

    public class Event
    {
        public string name { get; set; }        
        public DateTime date { get; set; }
        public List<Parameter> parameters { get; set; }
    }

    public class Root
    {
        public List<Event> events { get; set; }
    }


}
