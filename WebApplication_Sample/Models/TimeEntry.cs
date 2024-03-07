
#nullable disable

namespace WebApplication_Sample.Models
{
    public class TimeEntry
    {
        public string Id { get; set; }
        public string EmployeeName { get; set; }
        public DateTime StartTimeUtc { get; set; }
        public DateTime EndTimeUtc { get; set; }
        public string EntryNotes { get; set; }
        public DateTime? DeletedOn { get; set; }
    }
}