using Autogardener.Model;
using Autogardener.Model.Designs;
using Autogardener.Model.Plots;
using DalamudBasics.Logging;
using DalamudBasics.SaveGames;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public void AddNewDesign(Plot basePlot)
        {
            var newDesign = new PlotPlan();
            for (int i = 0; i < basePlot.PlantingHoles.Count; i++)
            {
                newDesign.PlotHolePlans.Add(CreateFromPlotHole(basePlot.PlantingHoles[i], i));
            }

            var save = saveManager.LoadCharacterSave();
            save.Designs.Add(newDesign);
            saveManager.WriteSave(save);
        }

        // Order
        // 7 6 5
        // 0 X 4
        // 1 2 3
        // Each rotation is 90º clockwise
        public void ApplyDesign(PlotPlan design, Plot targetPlot, int rotations)
        {
            if (design.PlotHolePlans.Count != targetPlot.PlantingHoles.Count)
            {
                throw new Exception("Mismatched design and plot hole count");
            }

            targetPlot.AppliedDesign = design;
            for (int i = 0; i < design.PlotHolePlans.Count; i++)
            {
                targetPlot.PlantingHoles[ApplyRotation(i, rotations)].Design = design.PlotHolePlans[i];
            }
        }

        private int ApplyRotation(int index, int rotations)
        {
            return Math.Abs(index - (rotations * 3)) % 8;
        }

        private PlotHolePlan CreateFromPlotHole(PlotHole basePlotHole, int index)
        {
            return new PlotHolePlan()
            {
                DesignatedPlant = basePlotHole.CurrentPlant,
                DesignatedSoil = 0,
                DoNotHarvest = false,
                RelativeIndex = index
            };
        }
    }
}
