using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Model.Plot.Plot
{
    public enum PlantStatus
    {
        NotPlanted,
        Growing,
        Wilted, // Going bad
        Withering, // Dead
        FullyGrown
    }
}
