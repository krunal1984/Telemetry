using System.Collections.Generic;

namespace TelemetryLib
{
    public class Schema
    {      
        
        
        public string EventName { get ; set; }
        public bool ImmidiateSend { get; set; }
        public List <Param>  Params { get; set; }

        public bool SendSystemSnapshot { get; set; }


    }

    public partial class Param
    {
        public string Name { get; set; }
        public ParameterType Type  {get; set;}
        public bool Mandatory  { get; set; }
        public string Size { get; set; }
        public string Default { get; set; }
    }

    public enum ParameterType 
    {
        Text = 1,
        Number = 2,
        Bool = 3,
        Double = 4,
    }
}