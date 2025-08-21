using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules.Schedulers
{
    public enum AbortReason
    {
        RetriesExceeded,
        MovedTooFarAway,
        UserRequest
    }
}
