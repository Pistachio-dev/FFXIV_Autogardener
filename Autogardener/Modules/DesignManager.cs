using Autogardener.Model;
using Autogardener.Model.Designs;
using Autogardener.Model.Plots;
using DalamudBasics.Logging;
using DalamudBasics.SaveGames;

namespace Autogardener.Modules
{
    public class DesignManager
    {
        private readonly ILogService logService;
        private readonly GlobalData globalData;
        private readonly SaveManager<CharacterSaveState> saveManager;

        public DesignManager(ILogService logService, GlobalData globalData, SaveManager<CharacterSaveState> saveManager)
        {
            this.logService = logService;
            this.globalData = globalData;
            this.saveManager = saveManager;
        }

        public void AddNewDesign(PlotPatch basePlot)
        {
            var newDesign = new PlotPatchDesign();
            for (int i = 0; i < basePlot.Plots.Count; i++)
            {
                newDesign.PlotDesigns.Add(CreateFromPlot(basePlot.Plots[i], i));
            }

            var save = saveManager.GetCharacterSaveInMemory();
            save.Designs.Add(newDesign);
            saveManager.WriteSave(save);
        }

        // Order
        // 7 6 5
        // 0 X 4
        // 1 2 3
        // Each rotation is 90º clockwise
        public void ApplyDesign(PlotPatchDesign design, PlotPatch targetPlot, int rotations)
        {
            if (design.PlotDesigns.Count != targetPlot.Plots.Count)
            {
                throw new Exception("Mismatched design and plot hole count");
            }

            targetPlot.AppliedDesign = new AppliedPlotPatchDesign(design);
            for (int i = 0; i < design.PlotDesigns.Count; i++)
            {
                targetPlot.Plots[ApplyRotation(i, rotations)].Design = design.PlotDesigns[i];
            }
        }

        private int ApplyRotation(int index, int rotations)
        {
            return Math.Abs(index - (rotations * 3)) % 8;
        }

        private PlotDesign CreateFromPlot(Plot basePlot, int index)
        {
            return new PlotDesign(index)
            {
                DesignatedSeed = basePlot.CurrentSeed,
                DesignatedSoil = 0,
                DoNotHarvest = false,
                RelativeIndex = index
            };
        }
    }
}
