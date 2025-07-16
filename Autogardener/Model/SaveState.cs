using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Model
{
    internal class SaveState
    {
        // The key is the character full name "John Smith@Omega"
        public Dictionary<string, CharacterSaveState> SaveStates { get; set; } = new();
    }
}
