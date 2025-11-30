// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace MyTranslateExtension;

public partial class MyTranslateExtensionCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;
    private readonly SettingsManager _settings = new();

    public MyTranslateExtensionCommandsProvider()
    {
        DisplayName = "Oishii Translate";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Settings = _settings.Settings;
        _commands = [
            new CommandItem(new MyTranslateExtensionPage(_settings)) { Title = DisplayName },
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

}
