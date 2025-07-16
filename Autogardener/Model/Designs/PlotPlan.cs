using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Model.Designs
{
    public class PlotPlan
    {
        public string PlanName { get; set; } = "Unnamed plan";

        public string PlanDescription { get; set; } = string.Empty;

        List<PlotHolePlan> PlotHolePlans { get; set; } = new();
    }
}
