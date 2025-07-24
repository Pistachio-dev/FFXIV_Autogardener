using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using DalamudBasics.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules
{
    public class ErrorMessageMonitor
    {
        private const int RecordSizeLimit = 20;
        private static readonly TimeSpan Recent = TimeSpan.FromSeconds(0.5f);
        private readonly ILogService logService;
        private readonly IChatGui chatGui;
        private readonly IFramework framework;

        public ErrorMessageMonitor(ILogService logService, IChatGui chatGui)
        {
            this.logService = logService;
            this.chatGui = chatGui;
        }

        public void Attach()
        {
            chatGui.ChatMessage += RecordErrorMessages;
        }

        public bool WasThereARecentError(string errorMessage = null)
        {
            DateTime now = DateTime.UtcNow;
            var recentMessages = recordedMessages.Where(r => now - r.DateTimeUtc < Recent);
            if (errorMessage != null)
            {
                recentMessages = recentMessages.Where(r => r.Message.Contains(errorMessage, StringComparison.OrdinalIgnoreCase));
            }
            return recentMessages.Any();
        }

        private void RecordErrorMessages(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (type == XivChatType.ErrorMessage || type == XivChatType.SystemError)
            {
                RecordMessage(message);
            }
        }

        private List<RecordedError> recordedMessages = new List<RecordedError>();
        
        private void RecordMessage(SeString message)
        {
            var record = new RecordedError() { Message = message.ToString(), DateTimeUtc = DateTime.UtcNow };
            recordedMessages.Add(record);
            if (recordedMessages.Count > RecordSizeLimit)
            {
                recordedMessages = recordedMessages.Skip(90).ToList();
            }
        }

        private class RecordedError
        {
            public string Message { get; set; }

            public DateTime DateTimeUtc { get; set; }
        }
    }
}
