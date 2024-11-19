using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using DalamudBasics.Configuration;
using DalamudBasics.GUI.Windows;
using DalamudBasics.Logging;
using ImGuiNET;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectTemplate.Windows;

public class ConfigWindow : PluginWindowBase, IDisposable
{
    private IConfiguration configuration;

    public ConfigWindow(ILogService logService, IServiceProvider sp) : base(logService, "Configuration")
    {
        Size = new Vector2(232, 90);
        SizeCondition = ImGuiCond.Always;

        configuration = sp.GetRequiredService<IConfiguration>();
    }

    public void Dispose() { }

    public override void PreDraw()
    {
    }

    protected override void SafeDraw()
    {
        
    }
}
