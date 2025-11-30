// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using MyTranslateExtension.Model;
using MyTranslateExtension.Pages;
using MyTranslateExtension.Translation;

namespace MyTranslateExtension;

internal sealed partial class MyTranslateExtensionPage : DynamicListPage, IDisposable
{
    private readonly List<ListItem> _items = [new ListItem(new NoOpCommand()) { Title = "TODO: Implement your extension here" }];
    private readonly SettingsManager _settings;
    private readonly TranslatorWorker _translator;
    private readonly DelayedNotifier _notifier;
    private static Task<TranslationResultDTO>? translationTask;
    private string oldSearchQuery = string.Empty;
    private string searchQuery = string.Empty;


    public MyTranslateExtensionPage(SettingsManager settings)
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "Oishii Translate";
        Name = "Open";
        _settings = settings;
        _translator = new(settings);
        _notifier = new(async ct => await UpdateResultsAsync(oldSearchQuery, searchQuery, ct), 50);
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        if (string.IsNullOrWhiteSpace(newSearch))
        {
            _items.Clear();
            _items.AddRange(_settings.LoadHistory());
            RaiseItemsChanged(_items.Count);
            return;
        }

        if (newSearch == oldSearch)
        {
            return;
        }

        oldSearchQuery = oldSearch;
        searchQuery = newSearch;

        _notifier.NotifyUpdate();
    }

    private async Task UpdateResultsAsync(string oldSearch, string newSearch, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(newSearch))
        {
            _items.Clear();
            _items.AddRange(_settings.LoadHistory());
            RaiseItemsChanged(_items.Count);
            return;
        }

        if (newSearch == oldSearch)
        {
            return;
        }

        var (provider, targetCode, text) = InputInterpreter.Parse(newSearch, _settings.DefaultTargetLanguage.ParseLangCode());

        try
        {
            if (translationTask == null || translationTask.IsCompleted) translationTask = _translator.TranslateAsync(targetCode, text, provider);
            var result = await translationTask.ConfigureAwait(false);

            _items.Clear();
            foreach (var item in result.Translations)
            {
                var translation = new TranslationEntity
                {
                    ProviderCode = item.ProviderCode,
                    OriginalText = text,
                    OriginalLangCode = item.DetectedSourceLanguage,
                    TranslatedText = item.Text.TrimStart(),
                    TargetLangCode = result.TargetLangCode,
                    Timestamp = DateTime.Now
                };

                var tagText = $"({translation.ProviderCode}): {translation.OriginalLangCode} -> {translation.TargetLangCode}";
                _items.Add(new ListItem(new ResultCopyCommand(translation, _settings))
                {
                    Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png"),
                    Title = translation.TranslatedText,
                    Subtitle = translation.OriginalText,
                    Tags = [new Tag(tagText)],
                });
            }
            RaiseItemsChanged(_items.Count);
        }
        catch (Exception)
        {
            _items.Clear();
            RaiseItemsChanged(_items.Count);
        }
    }

    public override IListItem[] GetItems() => [.. _items];

    public void Dispose()
    {
        ((IDisposable)_notifier).Dispose();
    }
}
