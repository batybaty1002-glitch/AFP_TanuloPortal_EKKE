using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TanuloPortal.Models;

namespace TanuloPortal.Services;

public class AiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public AiService(IConfiguration configuration)
    {
        _httpClient = new HttpClient();
        _apiKey = configuration["GeminiSettings:ApiKey"] 
                  ?? throw new InvalidOperationException("Hiányzik a GeminiSettings:ApiKey konfiguráció!");
        _model = configuration["GeminiSettings:Model"] ?? "gemini-1.5-flash";
    }

    public async Task<QuizResponse> GenerateQuizAsync(QuizRequest request)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

        var promptText = $@"Egy egyetemi oktató asszisztens vagy. Készíts egy kvízt a következő témában: '{request.Topic}'.
A kérdések pontos száma: {request.QuestionCount}.
A válaszod KIZÁRÓLAG érvényes JSON szöveg legyen, Markdown kódblokkok (pl. ```json) nélkül!
A JSON struktúrája pontosan ez legyen:
{{
  ""questions"": [
    {{
      ""question"": ""A kérdés szövege?"",
      ""options"": [""Válasz 1"", ""Válasz 2"", ""Válasz 3"", ""Válasz 4""],
      ""correctAnswerIndex"": 0
    }}
  ]
}}";

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = promptText }
                    }
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var errText = await response.Content.ReadAsStringAsync();
            throw new Exception($"Gemini API hiba ({response.StatusCode}): {errText}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var rootNode = JsonNode.Parse(jsonResponse);
        var rawGeneratedText = rootNode?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString() ?? "";

        var cleaned = rawGeneratedText.Trim();
        if (cleaned.StartsWith("```json")) cleaned = cleaned.Substring(7);
        if (cleaned.StartsWith("```")) cleaned = cleaned.Substring(3);
        if (cleaned.EndsWith("```")) cleaned = cleaned.Substring(0, cleaned.Length - 3);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var quiz = JsonSerializer.Deserialize<QuizResponse>(cleaned.Trim(), options);

        return quiz ?? new QuizResponse();
    }
}