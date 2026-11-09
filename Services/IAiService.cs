using TanuloPortal.Models;

namespace TanuloPortal.Services;

public interface IAiService
{
    Task<QuizResponse> GenerateQuizAsync(QuizRequest request);
}