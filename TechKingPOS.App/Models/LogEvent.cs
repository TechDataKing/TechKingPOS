using System;

namespace TechKingPOS.App.Models
{
    public class LogEvent
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Level { get; set; }     // INFO, ERROR, WARN
        public string Emoji { get; set; }
        public string Category { get; set; }  // Sale, Inventory, System
        public string Message { get; set; }
        public string Details { get; set; }   // JSON / stacktrace
    }
}
