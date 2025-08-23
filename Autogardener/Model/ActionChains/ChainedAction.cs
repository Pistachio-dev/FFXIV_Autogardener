using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Model.ActionChains
{
    public class ChainedAction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public ChainedActionType Type { get; set; }
        public string Command { get; set; } = string.Empty;

        public int CommandWaitTime { get; set; } = 1;
        public Guid PatchId { get; set; }
    }
}
