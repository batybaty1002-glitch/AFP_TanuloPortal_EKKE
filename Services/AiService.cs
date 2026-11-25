using System.Net.Http.Headers;
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
        _apiKey = configuration["GroqSettings:ApiKey"] 
                  ?? throw new InvalidOperationException("Hiányzik a GroqSettings:ApiKey konfiguráció!");
        _model = configuration["GroqSettings:Model"] ?? "openai/gpt-oss-120b";
    }

    public async Task<QuizResponse> GenerateQuizAsync(QuizRequest request)
    {
        var promptText = $@"Egy egyetemi oktató asszisztens vagy. Készíts egy magyar nyelvű kvízt a következő témában: '{request.Topic}'.
A kérdések pontos száma: {request.QuestionCount}.
A válaszod KIZÁRÓLAG érvényes JSON objektum legyen pontosan ebben a struktúrában:
{{
  ""questions"": [
    {{
      ""question"": ""A kérdés szövege?"",
      ""options"": [""Válasz 1"", ""Válasz 2"", ""Válasz 3"", ""Válasz 4""],
      ""correctAnswerIndex"": 0
    }}
  ]
}}";

        return await SendGroqQuizRequestAsync(promptText);
    }

    public async Task<QuizResponse> GenerateQuizFromTextAsync(string documentText, int questionCount)
    {
        var trimmedText = documentText.Length > 15000 
            ? documentText[..15000] 
            : documentText;

        var promptText = $@"Egy egyetemi oktató asszisztens vagy. Készíts egy magyar nyelvű kvízt KIZÁRÓLAG az alábbi tananyag szövege alapján! Ne használj külső információt, ami nincs benne a szövegben.
A kérdések pontos száma: {questionCount}.
A válaszod KIZÁRÓLAG érvényes JSON objektum legyen pontosan ebben a struktúrában:
{{
  ""questions"": [
    {{
      ""question"": ""A kérdés szövege?"",
      ""options"": [""Válasz 1"", ""Válasz 2"", ""Válasz 3"", ""Válasz 4""],
      ""correctAnswerIndex"": 0
    }}
  ]
}}

--- TANANYAG SZÖVEGE ---
{trimmedText}";

        return await SendGroqQuizRequestAsync(promptText);
    }

    private async Task<QuizResponse> SendGroqQuizRequestAsync(string promptText)
    {
        var url = "https://api.groq.com/openai/v1/chat/completions";

        var payload = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = "Mindig csak valid JSON-t válaszolj a megadott sémával." },
                new { role = "user", content = promptText }
            },
            response_format = new { type = "json_object" },
            temperature = 0.5
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(httpRequest);

        if (!response.IsSuccessStatusCode)
        {
            var errText = await response.Content.ReadAsStringAsync();
            throw new Exception($"Groq API hiba ({response.StatusCode}): {errText}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var rootNode = JsonNode.Parse(jsonResponse);
        var generatedJson = rootNode?["choices"]?[0]?["message"]?["content"]?.ToString() ?? "{}";

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var quiz = JsonSerializer.Deserialize<QuizResponse>(generatedJson, options);

        return quiz ?? new QuizResponse();
    }
}