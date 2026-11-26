using TanuloPortal.Models;

namespace TanuloPortal.Services;

public interface IAiService
{
    Task<QuizResponse> GenerateQuizAsync(QuizRequest request);
    Task<QuizResponse> GenerateQuizFromTextAsync(string documentText, int questionCount);
    Task<StudyPlanResponse> GenerateStudyPlanAsync(StudyPlanRequest request);
}