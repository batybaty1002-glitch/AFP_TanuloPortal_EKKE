using Microsoft.AspNetCore.Mvc;
using TanuloPortal.Models;
using TanuloPortal.Services;

namespace TanuloPortal.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IAiService _aiService;

    public AiController(IAiService aiService)
    {
        _aiService = aiService;
    }

    [HttpPost("generate-quiz")]
    public async Task<ActionResult<QuizResponse>> GenerateQuiz([FromBody] QuizRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Topic))
        {
            return BadRequest(new { message = "A téma megadása kötelező!" });
        }

        if (request.QuestionCount < 1 || request.QuestionCount > 10)
        {
            return BadRequest(new { message = "A kérdések száma 1 és 10 között lehet." });
        }

        try
        {
            var quiz = await _aiService.GenerateQuizAsync(request);
            return Ok(quiz);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Hiba történt az AI kvíz generálása közben.", details = ex.Message });
        }
    }
    [HttpPost("generate-quiz-from-file")]
    public async Task<IActionResult> GenerateQuizFromFile(IFormFile file, [FromForm] int questionCount = 5)
{
    if (file == null || file.Length == 0)
        return BadRequest(new { message = "Nem töltöttél fel fájlt, vagy a fájl üres." });

    if (questionCount < 1 || questionCount > 10)
        return BadRequest(new { message = "A kérdések száma 1 és 10 között lehet." });

    try
    {
        var extractedText = await DocumentTextExtractor.ExtractTextAsync(file);

        if (string.IsNullOrWhiteSpace(extractedText))
            return BadRequest(new { message = "Nem sikerült olvasható szöveget kinyerni a fájlból (pl. szkennelt kép alapú PDF)." });

        var quiz = await _aiService.GenerateQuizFromTextAsync(extractedText, questionCount);
        return Ok(quiz);
    }
    catch (NotSupportedException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "Hiba történt a fájl alapú kvíz generálása közben.", details = ex.Message });
    }
}
    
}