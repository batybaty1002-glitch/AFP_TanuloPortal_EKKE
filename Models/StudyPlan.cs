namespace TanuloPortal.Models;

public class StudyPlanRequest
{
    public string? FocusNote { get; set; }
    public bool IncludeCompletedTasks { get; set; } = false;
    public int MaxDays { get; set; } = 7;
}

public class StudyPlanItem
{
    public int Order { get; set; }
    public int? TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public string RecommendedDay { get; set; } = string.Empty;
    public string RecommendedTimeSlot { get; set; } = string.Empty;
    public int EstimatedMinutes { get; set; } = 45;
    public string Reason { get; set; } = string.Empty;
}

public class StudyPlanResponse
{
    public string Summary { get; set; } = string.Empty;
    public List<StudyPlanItem> Plan { get; set; } = new();
    public List<string> Tips { get; set; } = new();
}