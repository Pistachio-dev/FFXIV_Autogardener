using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using DalamudBasics.Chat.Listener;
using DalamudBasics.Debugging;
using DalamudBasics.DependencyInjection;
using DalamudBasics.Interop;
using DalamudBasics.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using Autogardener.Windows;
using ECommons;
using Autogardener.Modules;
using Autogardener.Modules.PlotRecognition;
using ECommons.Automation.LegacyTaskManager;

namespace Autogardener;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/autog";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("Autogardener");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    private IServiceProvider serviceProvider { get; init; }
    private ILogService logService { get; set; }

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        ECommonsMain.Init(pluginInterface, this);

        serviceProvider = BuildServiceProvider(pluginInterface);
        logService = serviceProvider.GetRequiredService<ILogService>();

        InitializeServices(serviceProvider);

        ConfigWindow = new ConfigWindow(logService, serviceProvider);
        MainWindow = new MainWindow(logService, serviceProvider);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        serviceProvider.GetRequiredService<ICommandManager>().AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Type /autog to start"
        });

        pluginInterface.UiBuilder.Draw += DrawUI;

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        pluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        // Adds another button that is doing the same but for the main ui of the plugin
        pluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
    }

    public void Dispose()
    {
        serviceProvider.GetRequiredService<HookManager>().Dispose();
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        serviceProvider.GetRequiredService<ICommandManager>().RemoveHandler(CommandName);        
    }

    private IServiceProvider BuildServiceProvider(IDalamudPluginInterface pluginInterface)
    {
        IServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddAllDalamudBasicsServices<Configuration>(pluginInterface);
        serviceCollection.AddSingleton<StringDebugUtils>();
        serviceCollection.AddSingleton<UtilOld>();
        serviceCollection.AddSingleton<Utils>();
        serviceCollection.AddSingleton<PlotWatcher>();
        serviceCollection.AddSingleton<TaskManager>();
        serviceCollection.AddSingleton<GlobalData>();
        serviceCollection.AddSingleton<Commands>();

        return serviceCollection.BuildServiceProvider();
    }

    private void InitializeServices(IServiceProvider serviceProvider)
    {
        IFramework framework = serviceProvider.GetRequiredService<IFramework>();
        serviceProvider.GetRequiredService<ILogService>().AttachToGameLogicLoop(framework);
        serviceProvider.GetRequiredService<IChatListener>().InitializeAndRun("[AG]");
        ActionWatcher.SetLogService(logService);
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
        ToggleMainUI();
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}
