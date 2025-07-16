using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Model.Plot
{
    public class Plot
    {
        public Plot(string alias)
        {
            Alias = alias;
        }

        public string Alias { get; set; }

        public List<PlotHole> PlantingHoles { get; set; } = new();

        public Vector3 Location => PlantingHoles.FirstOrDefault()?.Location ?? Vector3.Zero;
    }
}
