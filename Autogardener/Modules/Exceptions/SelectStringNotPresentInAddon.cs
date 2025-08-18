using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules.Exceptions
{
    internal class SelectStringNotPresentInAddon : Exception
    {
        internal SelectStringNotPresentInAddon(string expectedString, List<string> presentStrings): base()
        {
            ExpectedString= expectedString;
            PresentStrings= presentStrings;
        }

        public List<string> PresentStrings { get; }
        public string ExpectedString { get; }
    }
}
