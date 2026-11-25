using System.ComponentModel.DataAnnotations;

namespace TanuloPortal.Models
{
    public class TimetableItem
    {
        public int Id { get; set; }

        [Required]
        public string SubjectName { get; set; } = string.Empty;

        [Range(1, 7, ErrorMessage = "A nap értéke 1 (Hétfő) és 7 (Vasárnap) között lehet.")]
        public int DayOfWeek { get; set; }

        [Required]
        public string StartTime { get; set; } = string.Empty; 

        [Required]
        public string EndTime { get; set; } = string.Empty;   

        public string? Room { get; set; }

        public string? Teacher { get; set; }

        public string Color { get; set; } = "#3B82F6";
    }
}