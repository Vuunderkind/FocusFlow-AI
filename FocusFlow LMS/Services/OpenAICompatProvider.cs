using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FocusFlow_LMS.Models;

namespace FocusFlow_LMS.Services
{
    // ── Handles OpenAI, Mistral, Groq (all use same API schema) ──
    public class OpenAICompatProvider : ILLMProvider
    {
        private readonly Func<string> _apiKeyGetter;
        private readonly string       _baseUrl;
        private readonly ProviderType _type;
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(120) };

        public ProviderType ProviderType => _type;
        public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKeyGetter());

        public OpenAICompatProvider(ProviderType type, string baseUrl, Func<string> apiKeyGetter)
        {
            _type          = type;
            _baseUrl       = baseUrl;
            _apiKeyGetter  = apiKeyGetter;
        }

        public async Task<AIResponse> SendAsync(
            string systemPrompt, List<ChatMessage> history, string userMessage,
            string modelApiName, float temperature = 0.7f, int maxTokens = 4096,
            CancellationToken ct = default)
        {
            var key = _apiKeyGetter();
            if (string.IsNullOrWhiteSpace(key))
                return Fail($"{_type} API ключ не настроен.");

            var msgs = new List<object> { new { role = "system", content = systemPrompt } };
            foreach (var m in history)
                msgs.Add(new { role = m.Role, content = m.Content });
            msgs.Add(new { role = "user", content = userMessage });

            var body = new { model = modelApiName, messages = msgs, temperature, max_tokens = maxTokens };
            var req  = new HttpRequestMessage(HttpMethod.Post, _baseUrl);
            req.Headers.Add("Authorization", $"Bearer {key}");
            req.Content = new StringContent(JsonConvert.SerializeObject(body),
                System.Text.Encoding.UTF8, "application/json");

            try
            {
                var resp  = await _http.SendAsync(req, ct);
                var body2 = await resp.Content.ReadAsStringAsync(ct);
                if (!resp.IsSuccessStatusCode)
                {
                    var errMsg = JObject.Parse(body2)["error"]?["message"]?.ToString() ?? body2;
                    return Fail($"{_type} ({(int)resp.StatusCode}): {errMsg}");
                }
                var j = JObject.Parse(body2);
                return new AIResponse
                {
                    Text         = j["choices"]?[0]?["message"]?["content"]?.ToString() ?? "",
                    InputTokens  = j["usage"]?["prompt_tokens"]?.Value<int>()     ?? 0,
                    OutputTokens = j["usage"]?["completion_tokens"]?.Value<int>() ?? 0,
                    Model        = modelApiName,
                    Provider     = _type.ToString(),
                    Success      = true,
                };
            }
            catch (OperationCanceledException) { return Fail("Запрос отменён."); }
            catch (Exception ex)               { return Fail($"Ошибка сети: {ex.Message}"); }
        }

        private static AIResponse Fail(string e) => new() { Success = false, Error = e };
    }
}
