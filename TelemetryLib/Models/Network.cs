using System.ComponentModel.DataAnnotations;

namespace TelemetryLib.Models
{
    public class Network
    {
        [Required]
        public int Type { get; set; }
        [Required]
        public string Address { get; set; }
    }
}