using Dalamud.Game.Text;
using DalamudBasics.Configuration;

namespace Autogardener;

[Serializable]
public class Configuration : IConfiguration
{
    public int Version { get; set; } = 0;
    public XivChatType DefaultOutputChatType { get; set; } = XivChatType.Party;
    public bool LogOutgoingChatOutput { get; set; } = true;
    public bool LogClientOnlyChatOutput { get; set; } = true;
    public int LimitedChatChannelsMessageDelayInMs { get; set; } = 1000;

    public bool UseFertilizer { get; set; } = true;

    public bool Replant { get; set; } = true;

    public bool ShowOnlyItemsInInventory { get; set; } = true;

    public int StepDelayInMs { get; set; } = 500;
}
