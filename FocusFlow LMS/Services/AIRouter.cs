using FocusFlow_LMS.Models;

namespace FocusFlow_LMS.Services
{
    // ════════════════════════════════════════════════════════════
    //  AIRouter — Интеллектуальный маршрутизатор запросов
    //
    //  Логика AUTO режима:
    //  1. Классифицирует запрос (код / творческий / анализ / ...)
    //  2. Оценивает сложность (токены, вложенность)
    //  3. Выбирает оптимальный провайдер: лучший ответ + мин. стоимость
    //  4. Если включён FUSION — выбирает 2 модели для объединения
    // ════════════════════════════════════════════════════════════
    public class AIRouter
    {
        private readonly ProviderRegistry _registry;

        // ── Query type keywords (RU + EN) ─────────────────────────
        private static readonly string[] _codeKeywords =
        {
            "код", "code", "функция", "function", "class", "метод", "method",
            "баг", "bug", "ошибка", "error", "debug", "алгоритм", "algorithm",
            "python", "c#", "java", "javascript", "typescript", "sql", "html",
            "css", "react", "напиши программу", "напиши скрипт", "реализуй",
            "implement", "refactor", "рефактор", "api", "endpoint", "regex",
        };

        private static readonly string[] _mathKeywords =
        {
            "посчитай", "вычисли", "calculate", "решение", "уравнение",
            "equation", "формула", "formula", "математика", "math", "сумма",
            "процент", "probability", "вероятность", "интеграл", "derivative",
        };

        private static readonly string[] _creativeKeywords =
        {
            "напиши рассказ", "напиши стихотворение", "поэма", "poem",
            "story", "рассказ", "creative", "творческий", "слоган", "slogan",
            "маркетинговый текст", "копирайтинг", "copywriting", "статья",
            "blog post", "блог пост", "email", "письмо", "описание товара",
        };

        private static readonly string[] _analysisKeywords =
        {
            "проанализируй", "analyse", "analyze", "сравни", "compare",
            "оцени", "evaluate", "плюсы и минусы", "pros and cons", "swot",
            "стратегия", "strategy", "отчёт", "report", "исследование",
            "research", "почему", "why", "объясни", "explain",
        };

        public AIRouter(ProviderRegistry registry) => _registry = registry;

        // ── Classify the query ────────────────────────────────────
        public QueryType Classify(string message)
        {
            var lower = message.ToLowerInvariant();

            if (_codeKeywords.Any(k => lower.Contains(k)))     return QueryType.Code;
            if (_mathKeywords.Any(k => lower.Contains(k)))     return QueryType.Math;
            if (_creativeKeywords.Any(k => lower.Contains(k))) return QueryType.Creative;
            if (_analysisKeywords.Any(k => lower.Contains(k))) return QueryType.Analysis;

            // Complexity by length
            if (message.Length > 500) return QueryType.Analysis;
            if (message.Length < 80)  return QueryType.Simple;

            return QueryType.Unknown;
        }

        // ── Route: pick best available model(s) ──────────────────
        public RouteDecision Route(string message, OrchestrationMode mode)
        {
            var qtype     = Classify(message);
            var available = _registry.GetAvailableModels();
            var decision  = new RouteDecision { QueryType = qtype, Mode = mode };

            if (!available.Any())
            {
                decision.Reasoning = "Нет настроенных провайдеров. Добавьте API ключи в Настройках.";
                return decision;
            }

            if (mode == OrchestrationMode.Manual)
            {
                // Caller handles model selection
                decision.Reasoning = "Ручной режим: модель выбрана пользователем.";
                return decision;
            }

            if (mode == OrchestrationMode.Fusion)
            {
                // Pick 2 diverse high-quality models
                decision.Models   = PickFusionPair(available, qtype);
                decision.Reasoning = BuildFusionReason(decision.Models, qtype);
                return decision;
            }

            // ── AUTO: pick single best model ──────────────────────
            var best = PickBest(available, qtype);
            if (best != null) decision.Models.Add(best);
            decision.Reasoning = BuildAutoReason(best, qtype);
            return decision;
        }

        // ── Pick best single model ────────────────────────────────
        private static ModelInfo? PickBest(List<ModelInfo> available, QueryType qtype)
        {
            // Score each available model
            var scored = available
                .Select(m => (model: m, score: Score(m, qtype)))
                .OrderByDescending(x => x.score)
                .ToList();

            return scored.FirstOrDefault().model;
        }

        private static int Score(ModelInfo m, QueryType qtype)
        {
            int s = m.QualityScore * 10;

            // Boost for query-type match
            s += qtype switch
            {
                QueryType.Code     => (m.IsCodeExpert ? 30 : 0),
                QueryType.Math     => (m.IsMath       ? 30 : 0),
                QueryType.Creative => (m.IsCreative   ? 25 : 0),
                QueryType.Analysis => (m.IsAnalysis   ? 25 : 0),
                QueryType.Simple   => (m.IsFast       ? 20 : 0) - (int)(m.CostPer1K * 10000),
                _                  => 0,
            };

            // Cost penalty (prefer cheaper for simple tasks)
            if (qtype == QueryType.Simple || qtype == QueryType.Unknown)
                s -= (int)(m.CostPer1K * 5000);

            return s;
        }

        // ── Pick 2 models for fusion (different providers) ────────
        private static List<ModelInfo> PickFusionPair(List<ModelInfo> available, QueryType qtype)
        {
            var scored = available
                .Select(m => (model: m, score: Score(m, qtype)))
                .OrderByDescending(x => x.score)
                .ToList();

            var result = new List<ModelInfo>();
            var seenProviders = new HashSet<ProviderType>();

            foreach (var (model, _) in scored)
            {
                if (seenProviders.Add(model.Provider))
                {
                    result.Add(model);
                    if (result.Count == 2) break;
                }
            }

            // If only one provider available, just take top-2
            if (result.Count < 2)
                result = scored.Take(2).Select(x => x.model).ToList();

            return result;
        }

        private static string BuildAutoReason(ModelInfo? m, QueryType qtype)
        {
            if (m == null) return "Нет доступных моделей.";
            var typeLabel = qtype switch
            {
                QueryType.Code     => "задачи по программированию",
                QueryType.Math     => "математических вычислений",
                QueryType.Creative => "творческого контента",
                QueryType.Analysis => "глубокого анализа",
                QueryType.Simple   => "быстрого ответа (экономия ресурсов)",
                _                  => "универсального ответа",
            };
            return $"AUTO выбрал {m.Emoji} {m.DisplayName} — лучший для {typeLabel}.";
        }

        private static string BuildFusionReason(List<ModelInfo> models, QueryType qtype)
        {
            var names = string.Join(" + ", models.Select(m => $"{m.Emoji} {m.DisplayName}"));
            return $"FUSION: {names} → синтез лучшего ответа.";
        }
    }
}
