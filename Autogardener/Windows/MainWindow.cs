using Autogardener.Modules;
using DalamudBasics.GUI.Windows;
using DalamudBasics.Logging;
using ECommons.ChatMethods;
using ECommons.UIHelpers.AddonMasterImplementations;
using ImGuiNET;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Numerics;

namespace Autogardener.Windows;

public class MainWindow : PluginWindowBase, IDisposable
{
    Commands commands;
    public MainWindow(ILogService logService, IServiceProvider serviceProvider)
        : base(logService, "Autogardener", ImGuiWindowFlags.AlwaysAutoResize)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        commands = serviceProvider.GetRequiredService<Commands>();
    }

    public void Dispose() { }

    protected override unsafe void SafeDraw()
    {
        DrawActionButton(() => commands.DescribeTarget(), "Describe target");
        DrawActionButton(() => commands.InteractWithTargetPlot(), "Interact with plot");
        DrawActionButton(() => commands.ListCurrentMenuOptions(), "List current menu options");
        DrawActionButton(() => commands.SelectEntry("Quit"), "Select Quit");
        DrawActionButton(() => commands.SelectEntry("Harvest Crop"), "Select Harvest Crop");
        DrawActionButton(() => commands.SelectEntry("Plant Seeds"), "Select Plant Seeds");
        DrawActionButton(() => commands.TryDetectGardeningWindow(out var _), "Detect gardening window");
        DrawActionButton(() => commands.ClickCancelOnGardeningWindow(), "Click cancel in gardening window");
    }
}
