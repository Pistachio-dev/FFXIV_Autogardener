using Autogardener.Model;
using Autogardener.Modules;
using Dalamud.Plugin.Services;
using DalamudBasics.Configuration;
using DalamudBasics.GUI.Windows;
using DalamudBasics.Logging;
using DalamudBasics.SaveGames;
using ECommons.Automation.NeoTaskManager;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Autogardener.Windows.MainWindow;

public partial class MainWindow : PluginWindowBase, IDisposable
{
    private UtilOld oldUtil;
    private PlotWatcher plotWatcher;
    private GlobalData globalData;
    private Commands commands;
    private StoredDataActions storedDataActions;
    InGameActions inGameActions;
    private ISaveManager<CharacterSaveState> saveManager;
    private TaskManager taskManager;
    private ITextureProvider textureProvider;
    private IFramework framework;
    private IChatGui chatGui;
    public IConfigurationService<Configuration> configService;
    
    private static readonly Vector4 LightGreen = new Vector4(0.769f, 0.9f, 0.6f, 1);
    private static readonly Vector4 MidLightGreen = new Vector4(0.58f, 0.75f, 0.37f, 1);
    private static readonly Vector4 NeutralGreen = new Vector4(0.42f, 0.6f, 0.2f, 1);
    private static readonly Vector4 MidDarkGreen = new Vector4(0.278f, 0.455f, 0.075f, 1);
    private static readonly Vector4 DarkGreen = new Vector4(0.161f, 0.302f, 0, 1);
    private static readonly Vector4 NeutralBrown = new Vector4(0.651f, 0.49f, 0.196f, 1);
    private static readonly Vector4 MidDarkBrown = new Vector4(0.494f, 0.341f, 0.067f, 1);
    private static readonly Vector4 Red = new Vector4(0.55f, 0, 0, 1);
    private readonly string scarecrowPicturePath;

    public MainWindow(ILogService logService, IServiceProvider serviceProvider, string scarecrowPicturePath)
        : base(logService, "Autogardener", ImGuiWindowFlags.AlwaysAutoResize)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        oldUtil = serviceProvider.GetRequiredService<UtilOld>();
        plotWatcher = serviceProvider.GetRequiredService<PlotWatcher>();
        globalData = serviceProvider.GetRequiredService<GlobalData>();
        commands = serviceProvider.GetRequiredService<Commands>();
        storedDataActions = serviceProvider.GetRequiredService<StoredDataActions>();

        saveManager = serviceProvider.GetRequiredService<ISaveManager<CharacterSaveState>>();
        textureProvider = serviceProvider.GetRequiredService<ITextureProvider>();
        this.scarecrowPicturePath = scarecrowPicturePath;
        framework = serviceProvider.GetRequiredService<IFramework>();
        taskManager = serviceProvider.GetRequiredService<TaskManager>();
        chatGui = serviceProvider.GetRequiredService<IChatGui>();
        configService = serviceProvider.GetRequiredService<IConfigurationService<Configuration>>();
        inGameActions = serviceProvider.GetRequiredService<InGameActions>();
        
        framework.RunOnFrameworkThread(() =>
        {
            saveManager.GetCharacterSaveInMemory();
        });
        
    }

    protected override unsafe void SafeDraw()
    {
        if (ImGui.BeginTabBar("MainTabBar"))
        {
            var save = saveManager.GetCharacterSaveInMemory();
            if (ImGui.BeginTabItem("Plots"))
            {

                DrawPlotsTab(save);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Designs"))
            {
                DrawDesignsTab(save);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Other"))
            {
                DrawAssortedActions();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }
    private void DrawCenterHole()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);
        ImGui.BeginChildFrame(9, new Vector2(200, 200));
        var scarecrowPic = textureProvider.GetFromFile(scarecrowPicturePath).GetWrapOrDefault();
        if (scarecrowPic != null)
        {
            ImGui.Image(scarecrowPic.ImGuiHandle, new Vector2(scarecrowPic.Width, scarecrowPic.Height));
        }
        
        ImGui.EndChildFrame();
        ImGui.PopStyleVar();
    }

    private int[][] GetPlotLayout(int plotCount)
    {
        switch (plotCount)
        {
            case 1:
                return [[1]]; // Pot
            case 4:
                return [[1, 2], [3, 4]]; // Round garden patch
            case 6:
                return [[1, 2, 3], [4, 5, 6]]; // Oblong garden patch
            case 8:
                return [[7, 6, 5],
                            [0, 9 ,4],
                            [1, 2, 3]];
            default:
                return [];
        }
    }
    public void Dispose()
    { }   
}
