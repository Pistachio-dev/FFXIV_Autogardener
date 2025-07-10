using Autogardener.Modules;
using DalamudBasics.GUI.Windows;
using DalamudBasics.Logging;
using ECommons.ChatMethods;
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

    protected override void SafeDraw()
    {
        DrawActionButton(() => commands.DescribeTarget(), "Describe target");
        DrawActionButton(() => commands.InteractWithTargetPlot(), "Interact with plot");
    }
}
