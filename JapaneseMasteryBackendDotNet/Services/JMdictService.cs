using System.Text.Json;
using JapaneseMasteryBackendDotNet.Models;

namespace JapaneseMasteryBackendDotNet.Services;

public class JMdictService
{
    private readonly Dictionary<int, List<Vocabulary>> _levelVocabMap = new();
    private readonly object _lock = new();
    private readonly ILogger<JMdictService> _logger;

    public JMdictService(ILogger<JMdictService> logger)
    {
        _logger = logger;
        ReloadJMdict();
    }

    public void ReloadJMdict()
    {
        lock (_lock)
        {
            try
            {
                _levelVocabMap.Clear();

                var baseDir = Directory.GetCurrentDirectory();
                var dirInfo = new DirectoryInfo(baseDir);
                if (dirInfo.Name.Equals("JapaneseMasteryBackendDotNet", StringComparison.OrdinalIgnoreCase) ||
                    dirInfo.Name.Equals("JapaneseMasteryBackend", StringComparison.OrdinalIgnoreCase))
                {
                    baseDir = dirInfo.Parent?.FullName ?? baseDir;
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // 1. Load default_vocab.json (Levels 1-24)
                var defaultJsonFile = Path.Combine(baseDir, "JapaneseMastery", "data", "default_vocab.json");
                if (File.Exists(defaultJsonFile))
                {
                    _logger.LogInformation("Loading default_vocab.json from {Path}...", defaultJsonFile);
                    var json = File.ReadAllText(defaultJsonFile);
                    var defaultMap = JsonSerializer.Deserialize<Dictionary<string, List<Vocabulary>>>(json, options);
                    if (defaultMap != null)
                    {
                        foreach (var (k, v) in defaultMap)
                        {
                            if (int.TryParse(k, out var lvl))
                            {
                                _levelVocabMap[lvl] = v;
                            }
                        }
                    }
                }

                // 2. Load jmdict_levels.json (Levels 25+)
                var levelsJsonFile = Path.Combine(baseDir, "JapaneseMastery", "data", "jmdict_levels.json");
                if (File.Exists(levelsJsonFile))
                {
                    _logger.LogInformation("Loading jmdict_levels.json from {Path}...", levelsJsonFile);
                    var json = File.ReadAllText(levelsJsonFile);
                    var levelsMap = JsonSerializer.Deserialize<Dictionary<string, List<Vocabulary>>>(json, options);
                    if (levelsMap != null)
                    {
                        foreach (var (k, v) in levelsMap)
                        {
                            if (int.TryParse(k, out var lvl))
                            {
                                _levelVocabMap[lvl] = v;
                            }
                        }
                    }
                }

                _logger.LogInformation("Loaded vocabularies for {Count} levels.", _levelVocabMap.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load vocabulary JSON files.");
            }
        }
    }

    public List<Vocabulary> GetVocabulariesForLevel(int level)
    {
        lock (_lock)
        {
            if (!_levelVocabMap.TryGetValue(level, out var levelVocabs) || levelVocabs == null)
            {
                return new List<Vocabulary>();
            }

            var result = new List<Vocabulary>();
            foreach (var baseVocab in levelVocabs)
            {
                result.Add(new Vocabulary
                {
                    Id = baseVocab.Id,
                    WordJp = baseVocab.WordJp,
                    Kana = baseVocab.Kana,
                    Romaji = baseVocab.Romaji,
                    Meaning = baseVocab.Meaning,
                    Type = baseVocab.Type,
                    LevelRequired = level
                });
            }
            return result;
        }
    }

    public List<Vocabulary> GetAllVocabularies()
    {
        lock (_lock)
        {
            var all = new List<Vocabulary>();
            var sortedLevels = _levelVocabMap.Keys.OrderBy(k => k);
            foreach (var level in sortedLevels)
            {
                all.AddRange(GetVocabulariesForLevel(level));
            }
            return all;
        }
    }

    public List<Vocabulary> SearchVocabularies(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<Vocabulary>();

        var lowerQuery = query.Trim().ToLowerInvariant();
        var results = new List<Vocabulary>();

        lock (_lock)
        {
            foreach (var (level, vocabs) in _levelVocabMap)
            {
                foreach (var v in vocabs)
                {
                    if ((v.WordJp != null && v.WordJp.ToLowerInvariant().Contains(lowerQuery)) ||
                        (v.Kana != null && v.Kana.ToLowerInvariant().Contains(lowerQuery)) ||
                        (v.Romaji != null && v.Romaji.ToLowerInvariant().Contains(lowerQuery)) ||
                        (v.Meaning != null && v.Meaning.ToLowerInvariant().Contains(lowerQuery)))
                    {
                        results.Add(new Vocabulary
                        {
                            Id = v.Id,
                            WordJp = v.WordJp ?? string.Empty,
                            Kana = v.Kana,
                            Romaji = v.Romaji ?? string.Empty,
                            Meaning = v.Meaning ?? string.Empty,
                            Type = v.Type,
                            LevelRequired = level
                        });

                        if (results.Count >= 100) return results;
                    }
                }
            }
        }

        return results;
    }
}

