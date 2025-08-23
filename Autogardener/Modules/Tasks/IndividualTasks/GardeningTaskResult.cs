using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules.Tasks.IndividualTasks
{
    internal enum GardeningTaskResult
    {
        Complete,
        Incomplete,
        Bailout_RetriesExceeded,
        Bailout_Softbailout
    }
}
