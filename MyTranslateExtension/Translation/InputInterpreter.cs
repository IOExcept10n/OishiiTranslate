using System;
using System.Collections.Generic;
using System.Linq;
using MyTranslateExtension.Model;

namespace MyTranslateExtension.Translation
{
    public class InputInterpreter
    {
        private static readonly HashSet<string> KnownProviders = new(StringComparer.OrdinalIgnoreCase)
        {
            "google",
            "bing",
            "azure",
            "yandex",
            "deepl"
        };

        /// <summary>
        /// Parses the input to extract provider (optional), target language, and text
        /// Format: [provider] language text
        /// Example: "deepl ko hello world", "ko hello world", "hello world"
        /// </summary>
        public static (string? Provider, LangCode TargetCode, string Text) Parse(string query, LangCode defaultLangCode)
        {
            if (string.IsNullOrWhiteSpace(query))
                return (null, defaultLangCode, string.Empty);

            var parts = query.Split([' '], StringSplitOptions.RemoveEmptyEntries);
            string? provider = null;
            string? targetLangCode = null;
            string text = string.Join(' ', parts.Skip(1));  // Default text to everything after the first part

            // Check if there are any parts
            if (parts.Length > 0)
            {
                // First part might be a provider or a language code
                if (KnownProviders.Contains(parts[0], StringComparer.OrdinalIgnoreCase))
                {
                    provider = parts[0].ToLowerInvariant();  // Match provider case style
                    if (parts.Length > 1)
                    {
                        targetLangCode = parts[1];  // This can be a language code
                        text = string.Join(" ", parts.Skip(2));  // Text is everything after provider and language code
                    }
                }
                else
                {
                    // If the first part is not a provider, it must be a language code
                    targetLangCode = parts[0];
                }
            }

            // Determine the target language
            LangCode target;

            if (!string.IsNullOrEmpty(targetLangCode))
            {
                target = targetLangCode.ParseLangCode();
                if (target == LangCode.Unknown)
                {
                    // If language parsing failed, default to the default language
                    target = defaultLangCode;
                    text = query;  // Treat the whole query as the text
                }
            }
            else
            {
                // No language code specified, use the default language
                target = defaultLangCode;
            }

            return (provider, target, text.Trim());
        }

    }
}
