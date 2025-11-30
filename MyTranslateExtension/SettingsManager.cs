using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.CommandPalette.Extensions.Toolkit;
using MyTranslateExtension.Model;
using MyTranslateExtension.Properties;

namespace MyTranslateExtension
{
    public class SettingsManager : JsonSettingsManager
    {
        private const string Namespace = "oishii-translate";

        private readonly string _historyPath;
        private static readonly List<ChoiceSetSetting.Choice> _historyChoices = [new(Localization.None, Localization.None), new("1", "1"), new("3", "3"), new("5", "5"), new("10", "10"), new("20", "20")];
        private static readonly List<ChoiceSetSetting.Choice> _targetLanguages = [.. from v in Enum.GetValues<LangCode>() select new ChoiceSetSetting.Choice(v.ToNormalizedString(), ((int)v).ToString(CultureInfo.InvariantCulture))];

        private readonly ChoiceSetSetting _showHistory = new(ToName(nameof(ShowHistory)), Localization.HistoryTitle, Localization.HistoryDescriptionMessage, _historyChoices);
        private readonly ChoiceSetSetting _targetLanguage = new(ToName(nameof(DefaultTargetLanguage)), Localization.DefaultTargetLanguageCode, Localization.DefaultTargetLanguageCodeDescription, _targetLanguages);
        private readonly TextSetting _deeplApiKey = new(ToName(nameof(DeeplApiKey)), Localization.DeeplApiKey, Localization.DeeplApiKey, "DeepL-Auth-Key {API KEY}");

        private readonly TextSetting _translationTimeout = new(ToName(nameof(TranslationTimeout)), Localization.TranslationTimeoutTitle, Localization.TranslationTimeoutDescription, "2000");
        private readonly ToggleSetting _googleTranslateEnabled = new(ToName(nameof(GoogleTranslateEnabled)), Localization.GoogleTranslate, Localization.EnabledProviders, false);
        private readonly ToggleSetting _bingTranslateEnabled = new(ToName(nameof(BingTranslateEnabled)), Localization.BingTranslate, string.Empty, false);
        private readonly ToggleSetting _microsoftAzureTranslateEnabled = new(ToName(nameof(MicrosoftAzureTranslateEnabled)), Localization.MicrosoftAzureTranslate, string.Empty, false);
        private readonly ToggleSetting _yandexTranslateEnabled = new(ToName(nameof(YandexTranslateEnabled)), Localization.YandexTranslate, string.Empty, false);
        private readonly ToggleSetting _deeplTranslateEnabled = new(ToName(nameof(DeepLTranslateEnabled)), Localization.DeepLTranslate, string.Empty, false);

        public string ShowHistory => _showHistory.Value ?? string.Empty;
        public string DefaultTargetLanguage => _targetLanguage.Value ?? string.Empty;
        public string DeeplApiKey => _deeplApiKey.Value ?? string.Empty;
        public int TranslationTimeout => int.TryParse(_translationTimeout.Value, out int value) ? value : 2000;

        public bool GoogleTranslateEnabled => _googleTranslateEnabled.Value;
        public bool BingTranslateEnabled => _bingTranslateEnabled.Value;
        public bool MicrosoftAzureTranslateEnabled => _microsoftAzureTranslateEnabled.Value;
        public bool YandexTranslateEnabled => _yandexTranslateEnabled.Value;
        public bool DeepLTranslateEnabled => _deeplTranslateEnabled.Value;

        internal static string SettingsJsonPath()
        {
            var directory = Utilities.BaseSettingsPath("Microsoft.CmdPal");
            Directory.CreateDirectory(directory);

            return Path.Combine(directory, "settings.json");
        }

        internal static string HistoryStateJsonPath()
        {
            var directory = Utilities.BaseSettingsPath("Microsoft.CmdPal");
            Directory.CreateDirectory(directory);

            return Path.Combine(directory, "oishii_translate_history.json");
        }

        public void SaveHistory(TranslationEntity historyItem)
        {
            if (historyItem == null)
            {
                return;
            }

            try
            {
                List<TranslationEntity> historyItems;

                if (File.Exists(_historyPath))
                {
                    var existingContent = File.ReadAllText(_historyPath);
                    historyItems = JsonSerializer.Deserialize(existingContent, SourceGenerationContext.Default.ListTranslationEntity) ?? [];
                }
                else
                {
                    historyItems = [];
                }

                historyItems.Add(historyItem);

                historyItems = [.. historyItems.DistinctBy(x => x.TranslatedText)];

                if (int.TryParse(ShowHistory, out var maxHistoryItems) && maxHistoryItems > 0)
                {
                    while (historyItems.Count > maxHistoryItems)
                    {
                        historyItems.RemoveAt(0);
                    }
                }

                var historyJson = JsonSerializer.Serialize(historyItems, SourceGenerationContext.Default.ListTranslationEntity);
                File.WriteAllText(_historyPath, historyJson);
            }
            catch (Exception ex)
            {
                ExtensionHost.LogMessage(new LogMessage() { Message = ex.ToString() });
            }
        }

        public List<ListItem> LoadHistory()
        {
            try
            {
                if (!File.Exists(_historyPath))
                {
                    return [];
                }

                var fileContent = File.ReadAllText(_historyPath);
                var historyItems = JsonSerializer.Deserialize(fileContent, SourceGenerationContext.Default.ListTranslationEntity) ?? [];

                var listItems = new List<ListItem>();
                foreach (var historyItem in historyItems)
                {
                    try
                    {
                        if (historyItem == null)
                        {
                            ExtensionHost.LogMessage(new LogMessage() { Message = "Null history item found, skipping." });
                            continue;
                        }

                        if (historyItem.OriginalText == null ||
                            historyItem.TranslatedText == null ||
                            historyItem.OriginalLangCode == null ||
                            historyItem.TargetLangCode == null)
                        {
                            ExtensionHost.LogMessage(new LogMessage() { Message = "History item contains null fields, skipping." });
                            continue;
                        }

                        listItems.Add(new ListItem(new ResultCopyCommand(historyItem, this))
                        {
                            Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png"),
                            Title = historyItem.TranslatedText,
                            Subtitle = historyItem.OriginalText,
                            Tags = [new Tag($"{historyItem.OriginalLangCode} -> {historyItem.TargetLangCode}")],
                        });
                    }
                    catch (Exception ex)
                    {
                        ExtensionHost.LogMessage(new LogMessage() { Message = $"Error processing history item: {ex}" });
                    }
                }

                listItems.Reverse();
                return listItems;
            }
            catch (Exception ex)
            {
                ExtensionHost.LogMessage(new LogMessage() { Message = ex.ToString() });
                return [];
            }
        }

        public SettingsManager()
        {
            FilePath = SettingsJsonPath();
            _historyPath = HistoryStateJsonPath();

            Settings.Add(_showHistory);
            Settings.Add(_targetLanguage);
            Settings.Add(_deeplApiKey);
            Settings.Add(_translationTimeout);
            Settings.Add(_googleTranslateEnabled);
            Settings.Add(_bingTranslateEnabled);
            Settings.Add(_microsoftAzureTranslateEnabled);
            Settings.Add(_yandexTranslateEnabled);
            Settings.Add(_deeplTranslateEnabled);

            LoadSettings();

            Settings.SettingsChanged += (s, a) => SaveSettings();
        }

        private void ClearHistory()
        {
            try
            {
                if (File.Exists(_historyPath))
                {
                    File.Delete(_historyPath);

                    ExtensionHost.LogMessage(new LogMessage() { Message = "History cleared successfully." });
                }
                else
                {
                    ExtensionHost.LogMessage(new LogMessage() { Message = "No history file found to clear." });
                }
            }
            catch (Exception ex)
            {
                ExtensionHost.LogMessage(new LogMessage() { Message = $"Failed to clear history: {ex}" });
            }
        }

        public override void SaveSettings()
        {
            base.SaveSettings();
            try
            {
                if (ShowHistory == Localization.None)
                {
                    ClearHistory();
                }
                else if (int.TryParse(ShowHistory, out var maxHistoryItems) && maxHistoryItems > 0)
                {
                    if (File.Exists(_historyPath))
                    {
                        var existingContent = File.ReadAllText(_historyPath);
                        var historyItems = JsonSerializer.Deserialize(existingContent, SourceGenerationContext.Default.ListTranslationEntity) ?? [];

                        historyItems = [.. historyItems.DistinctBy(x => x.TranslatedText)];

                        if (historyItems.Count > maxHistoryItems)
                        {
                            historyItems = [.. historyItems.Skip(historyItems.Count - maxHistoryItems)];

                            var trimmedHistoryJson = JsonSerializer.Serialize(historyItems, SourceGenerationContext.Default.ListTranslationEntity);
                            File.WriteAllText(_historyPath, trimmedHistoryJson);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ExtensionHost.LogMessage(new LogMessage() { Message = ex.ToString() });
            }
        }

        private static string ToName(string propertyName) => $"{Namespace}.{propertyName}";
    }
}
