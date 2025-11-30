using System.Threading;
using System.Threading.Tasks;
using MyTranslateExtension.Model;

namespace MyTranslateExtension.Translation.Providers
{
    internal interface ITranslationProvider
    {
        string Name { get; }

        bool IsAvailable { get; }

        Task<TranslationResultDTO> TranslateAsync(string text, LangCode targetLanguage, CancellationToken cancellationToken);
    }
}
