using System;

namespace MyTranslateExtension.Model
{
    public enum LangCode
    {
        AR,      // Arabic
        BG,      // Bulgarian
        CS,      // Czech
        DA,      // Danish
        DE,      // German
        EL,      // Greek
        EN,      // English (unspecified variant for backward compatibility; please select EN-GB or EN-US instead)
        EN_GB,   // English (British)
        EN_US,   // English (American)
        ES,      // Spanish
        ET,      // Estonian
        FI,      // Finnish
        FR,      // French
        HU,      // Hungarian
        ID,      // Indonesian
        IT,      // Italian
        JA,      // Japanese
        KO,      // Korean
        LT,      // Lithuanian
        LV,      // Latvian
        NB,      // Norwegian Bokmål
        NL,      // Dutch
        PL,      // Polish
        PT,      // Portuguese (unspecified variant for backward compatibility; please select PT-BR or PT-PT instead)
        PT_BR,   // Portuguese (Brazilian)
        PT_PT,   // Portuguese (all Portuguese varieties excluding Brazilian Portuguese)
        RO,      // Romanian
        RU,      // Russian
        SK,      // Slovak
        SL,      // Slovenian
        SV,      // Swedish
        TR,      // Turkish
        UK,      // Ukrainian
        ZH,      // Chinese (simplified)
        Unknown,     // Unknown
    }

    static class LangCodeExtensions
    {
        public static LangCode GetLangCode(this int code)
        {
            if (code >= 0 && code < (int)LangCode.Unknown)
            {
                return (LangCode)code;
            }
            return LangCode.Unknown;
        }

        public static LangCode ParseLangCode(this string codeString)
        {
            switch (codeString)
            {
                case "gb":
                case "GB":
                case "EN-GB":
                case "EN_GB":
                    return LangCode.EN_GB;

                case "us":
                case "US":
                case "EN-US":
                case "EN_US":
                    return LangCode.EN_US;

                case "br":
                case "BR":
                case "PT-BR":
                case "PT_BR":
                    return LangCode.PT_BR;

                case "pt":
                case "PT":
                case "PT-PT":
                case "PT_PT":
                    return LangCode.PT_PT;
                default:
                    try
                    {
                        if (Enum.TryParse(codeString.ToUpperInvariant(), true, out LangCode langCode))
                            return langCode;
                    }
                    catch { }
                    return LangCode.Unknown;
            }
        }

        public static string ToNormalizedString(this LangCode code) => code.ToString().Replace('_', '-');
    }
}
