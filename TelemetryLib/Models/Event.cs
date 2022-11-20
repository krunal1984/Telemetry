using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelemetryLib.Models
{
    public class Event
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Id { get; set; }
        [Required]
        public string EventName { get; set; }

        [Required]
        public String EventParams { get; set; }

        [Required]
        public string SessionId { get; set; }

        public string UserSessionId { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        public bool sendImmediate { get; set; }        

    }
}
