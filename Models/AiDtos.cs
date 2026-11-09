namespace TanuloPortal.Models;

public class QuizRequest
{
    public string Topic { get; set; } = string.Empty;
    public int QuestionCount { get; set; } = 3;
}

public class QuizQuestionDto
{
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectAnswerIndex { get; set; }
}

public class QuizResponse
{
    public List<QuizQuestionDto> Questions { get; set; } = new();
}