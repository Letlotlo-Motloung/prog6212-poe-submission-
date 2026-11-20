using System.ComponentModel.DataAnnotations;

namespace RaceDay.Mvc.Models
{
    public class ResultEntryInput
    {
        public int EventID { get; set; }
        public int EnrolmentID { get; set; }
        public int? ResultID { get; set; }
        [Display(Name = "Finish time")] public string? FinishTime { get; set; }
        public int? Position { get; set; }
        public string Status { get; set; } = "Finished";
        public bool IsPublished { get; set; }
    }
}
