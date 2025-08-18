using Dalamud.Bindings.ImGui;
using DalamudBasics.Configuration;
using DalamudBasics.GUI.Windows;
using DalamudBasics.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Autogardener.Windows;

public class ConfigWindow : PluginWindowBase, IDisposable
{
    private IConfiguration configuration;

    public ConfigWindow(ILogService logService, IServiceProvider sp) : base(logService, "Configuration")
    {
        Size = new Vector2(232, 90);        

        configuration = sp.GetRequiredService<IConfiguration>();
    }

    public void Dispose()
    { }

    public override void PreDraw()
    {
    }

    protected override void SafeDraw()
    {
        ImGui.TextUnformatted("There is no configuration menu in Ba Sing Se");
    }
}
