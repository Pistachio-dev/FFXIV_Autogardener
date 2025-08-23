using Autogardener.Model.ActionChains;
using DalamudBasics.Chat.Output;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Autogardener.Modules.Schedulers
{
    internal class ChatCommandScheduler : IScheduler
    {
        private readonly ChainedAction action;
        private readonly IChatOutput chatOutput;
        private bool executed = false;
        private Stopwatch timer = new Stopwatch();
        public bool Done() => executed && timer.Elapsed >= TimeSpan.FromSeconds(action.CommandWaitTime);

        public ChatCommandScheduler(ChainedAction action, IChatOutput chatOutput)
        {
            this.action = action;
            this.chatOutput = chatOutput;
        }

        public void Tick()
        {
            if (!executed)
            {
                chatOutput.WriteCommand(action.Command);
                timer.Start();
                executed = true;
            }
        }

    }
}
