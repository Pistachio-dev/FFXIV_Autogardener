using Dalamud.Plugin.Services;
using DalamudBasics.Logging;
using System.Linq;
using System.Text.RegularExpressions;

namespace Autogardener.Modules.Actions
{
    public class DataExtraction
    {
        private readonly ILogService logService;
        private readonly GlobalData globalData;
        private readonly IClientState clientState;

        public DataExtraction(ILogService logService, GlobalData globalData, IClientState clientState)
        {
            this.logService = logService;
            this.globalData = globalData;
            this.clientState = clientState;
        }            

        public (uint id, string name) ExtractPlantNameAndId(string dialogueText)
        {
            var matches = new Regex("([\\w ]{4,})").Matches(dialogueText);
            if (matches.Count < 2 || matches[0].Groups.Count == 0)
            {
                logService.Info("Scanned plot was empty");
                return (0, "Empty");
            }
            var plantName = matches[0].Groups[0].Value;

            var result = SearchSeed(plantName);
            if (result.id == 0 && clientState.ClientLanguage == Dalamud.Game.ClientLanguage.English)
            {
                return ExtractXlightNameAndId(plantName);
            }

            return result;
        }

        // This does not work in french at all, but that's ok
        private (uint id, string name) ExtractXlightNameAndId(string plantName)
        {
            var regex = new Regex($"(\\w+)\\s{globalData.GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.Shard)}");
            var match = regex.Match(plantName);
            if (!match.Success || match.Groups.Count == 0)
            {
                return (0, "Empty");
            }
            var shardElement = match.Groups[0];
            var seedName = $"{shardElement}{globalData.GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.xLight)}";
            return SearchSeed(seedName);
        }

        private (uint id, string name) SearchSeed(string seedPartialName)
        {
            try
            {
                var seedDictionaryEntry = globalData.Seeds.First(s => s.Value.Name.ToString().Contains(seedPartialName, StringComparison.OrdinalIgnoreCase));
                return (seedDictionaryEntry.Key, seedDictionaryEntry.Value.Name.ToString());
            }
            catch (InvalidOperationException)
            {
                logService.Warning($"No seed matching the plant {seedPartialName} found");
                return (0, "Empty");
            }
        }
    }
}
