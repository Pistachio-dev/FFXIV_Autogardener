using Autogardener.Model;
using Autogardener.Modules;
using Autogardener.Windows;
using Autogardener.Windows.MainWindow;
using Dalamud.Game.Command;
using Dalamud.Plugin.Services;
using DalamudBasics.Chat.Listener;
using DalamudBasics.Debugging;
using DalamudBasics.DependencyInjection;
using DalamudBasics.Interop;
using DalamudBasics.Logging;
using DalamudBasics.SaveGames;
using ECommons.Automation.NeoTaskManager;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace Autogardener;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/autog";

    public CharacterSaveState State { get; private set; } = new CharacterSaveState();
    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("Autogardener");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    private IServiceProvider serviceProvider { get; init; }
    private ILogService logService { get; set; }

    private IClientState clientState { get; set; }

    private ISaveManager<CharacterSaveState> saveManager { get; set; }

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        ECommonsMain.Init(pluginInterface, this);

        serviceProvider = BuildServiceProvider(pluginInterface);
        logService = serviceProvider.GetRequiredService<ILogService>();
        clientState = serviceProvider.GetRequiredService<IClientState>();
        saveManager = serviceProvider.GetRequiredService<ISaveManager<CharacterSaveState>>();

        InitializeServices(serviceProvider);
        
        var scarecrowPicturePath = Path.Combine(pluginInterface.AssemblyLocation.Directory?.FullName!, "Scarecrow.png");
        ConfigWindow = new ConfigWindow(logService, serviceProvider);
        MainWindow = new MainWindow(logService, serviceProvider, scarecrowPicturePath);

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

    private void MakeSaveReady(IFramework framework)
    {
        if (State != null || clientState.LocalPlayer == null)
        {
            return;
        }

        State = saveManager.GetCharacterSaveInMemory();
    }

    public void Dispose()
    {
        serviceProvider.GetRequiredService<HookManager>().Dispose();
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        serviceProvider.GetRequiredService<ICommandManager>().RemoveHandler(CommandName);

        serviceProvider.GetRequiredService<IFramework>().Update -= MakeSaveReady;
    }

    private IServiceProvider BuildServiceProvider(IDalamudPluginInterface pluginInterface)
    {
        IServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddAllDalamudBasicsServices<Configuration>(pluginInterface);
        string saveFileName = Path.Combine(pluginInterface.GetPluginConfigDirectory(), "SavedData.json");
        serviceCollection.AddSingleton<ISaveManager<CharacterSaveState>>(
            (sp) => new SaveManager<CharacterSaveState>(saveFileName, logService, clientState));
        serviceCollection.AddSingleton<StringDebugUtils>();
        serviceCollection.AddSingleton<UtilOld>();
        serviceCollection.AddSingleton<Utils>();
        serviceCollection.AddSingleton<PlotWatcher>();
        serviceCollection.AddSingleton<TaskManager>();
        serviceCollection.AddSingleton<GlobalData>();
        serviceCollection.AddSingleton<Commands>();
        serviceCollection.AddSingleton<PlayerActions>();
        serviceCollection.AddSingleton<DesignManager>();
        return serviceCollection.BuildServiceProvider();
    }

    private void InitializeServices(IServiceProvider serviceProvider)
    {
        IFramework framework = serviceProvider.GetRequiredService<IFramework>();
        serviceProvider.GetRequiredService<ILogService>().AttachToGameLogicLoop(framework);
        serviceProvider.GetRequiredService<IChatListener>().InitializeAndRun("[AG]");
        framework.Update += MakeSaveReady;
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
