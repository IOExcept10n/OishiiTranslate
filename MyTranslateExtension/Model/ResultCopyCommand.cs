using System;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions.Toolkit;
using MyTranslateExtension.Model;
using MyTranslateExtension.Properties;

namespace MyTranslateExtension
{
    public partial class ResultCopyCommand : InvokableCommand
    {
        private readonly SettingsManager _settingsManager;

        public TranslationEntity Arguments { get; set; }

        public ResultCopyCommand(TranslationEntity arguments, SettingsManager settingsManager)
        {
            ArgumentNullException.ThrowIfNull(arguments);
            ArgumentNullException.ThrowIfNull(settingsManager);
            Arguments = arguments;
            _settingsManager = settingsManager;
            Name = "Copy";
        }

        public override CommandResult Invoke()
        {
            var task = ExecuteAsync();
            task.Wait();

            return CommandResult.Hide();
        }

        private async Task ExecuteAsync()
        {
            ClipboardHelper.SetText(Arguments.TranslatedText);

            if (_settingsManager.ShowHistory != Localization.None)
            {
                _settingsManager.SaveHistory(Arguments);
            }
        }
    }
}
