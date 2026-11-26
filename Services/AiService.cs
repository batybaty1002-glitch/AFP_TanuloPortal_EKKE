using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using TanuloPortal.Data;
using TanuloPortal.Models;

namespace TanuloPortal.Services;


public class AiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    private readonly string _apiKey;
    private readonly string _model;

    public AiService(IConfiguration configuration, AppDbContext context)
    {
        _httpClient = new HttpClient();
        _context = context;
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

    public async Task<StudyPlanResponse> GenerateStudyPlanAsync(StudyPlanRequest request)
    {
        var tasksQuery = _context.Tasks.AsNoTracking().AsQueryable();
        if (!request.IncludeCompletedTasks)
        {
            tasksQuery = tasksQuery.Where(t => t.Status.ToLower() != "done" && t.Status.ToLower() != "completed");
        }

        var tasks = await tasksQuery.OrderBy(t => t.Id).ToListAsync();
        var timetable = await _context.TimeTableItems
            .AsNoTracking()
            .OrderBy(t => t.DayOfWeek)
            .ThenBy(t => t.StartTime)
            .ToListAsync();

        var tasksText = tasks.Count == 0
            ? "Nincsenek aktuális feladatok."
            : string.Join("\n", tasks.Select(t =>
                $"- [Id: {t.Id}] Cím: \"{t.Title}\" | Leírás: \"{t.Description}\" | Státusz: {t.Status} | Prioritás: {t.Priority}"));

        var timetableText = timetable.Count == 0
            ? "Nincs rögzített órarendi elem."
            : string.Join("\n", timetable.Select(item =>
                $"- {GetHungarianDayName(item.DayOfWeek)}: {item.StartTime}-{item.EndTime} | Tantárgy: {item.SubjectName}" +
                (string.IsNullOrWhiteSpace(item.Room) ? "" : $" | Terem: {item.Room}")));

        var focusInstruction = string.IsNullOrWhiteSpace(request.FocusNote)
            ? "Nincs extra fókuszmegjegyzés."
            : $"Felhasználói kérés/fókusz: {request.FocusNote}";

        var promptText = $@"Egy egyetemi tanulási mentor vagy. Készíts egy reális, magyar nyelvű tanulási tervet a hallgató meglévő feladatai (prioritás és státusz szerint) ÉS az órarendje alapján!
Kerüld az órarendben foglalt órák idősávjait, és a magasabb prioritású (pl. High / Urgent) feladatokat helyezd előrébb.
Tervezési időtáv maximum napokban: {request.MaxDays}.
{focusInstruction}

--- MEGLÉVŐ FELADATOK ---
{tasksText}

--- ÓRAREND ---
{timetableText}

A válaszod KIZÁRÓLAG érvényes JSON objektum legyen pontosan ebben a struktúrában:
{{
  ""summary"": ""Rövid összefoglaló a tanulási terv stratégiájáról"",
  ""plan"": [
    {{
      ""order"": 1,
      ""taskId"": 1,
      ""taskTitle"": ""Feladat címe"",
      ""priority"": ""High"",
      ""recommendedDay"": ""Hétfő"",
      ""recommendedTimeSlot"": ""16:00 - 17:00"",
      ""estimatedMinutes"": 60,
      ""reason"": ""Miért ekkor és ebben a sorrendben érdemes haladni vele""
    }}
  ],
  ""tips"": [
    ""Hasznos tanulási tipp 1"",
    ""Hasznos tanulási tipp 2""
  ]
}}";

        return await SendGroqStudyPlanRequestAsync(promptText);
    }

    private static string GetHungarianDayName(int dayOfWeek) => dayOfWeek switch
    {
        1 => "Hétfő",
        2 => "Kedd",
        3 => "Szerda",
        4 => "Csütörtök",
        5 => "Péntek",
        6 => "Szombat",
        7 => "Vasárnap",
        _ => $"Nap ({dayOfWeek})"
    };

    private async Task<StudyPlanResponse> SendGroqStudyPlanRequestAsync(string promptText)
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
        var plan = JsonSerializer.Deserialize<StudyPlanResponse>(generatedJson, options);

        return plan ?? new StudyPlanResponse();
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