using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Model.Designs
{
    internal class PlotHolePlan
    {
        public uint DesignatedPlant { get; set; } //ItemId
        public uint DesignatedSoil { get; set; } //ItemId
        public bool DoNotHarvest { get; set; } // For those plants that you leave up, for interbreeding
        public int RelativeIndex { get; set; } // From zero to 7, the number is added to the actual plot id.

    }
}
