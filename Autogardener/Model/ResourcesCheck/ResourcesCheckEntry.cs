using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Model.ResourcesCheck
{
    public class ResourcesCheckEntry
    {
        public string ItemName { get; set; } = "I forgot to set the type oh no";

        public uint ItemId { get; set; }

        public int ExpectedAmount { get; set; }

        public int ActualAmount { get; set; }
    }
}
