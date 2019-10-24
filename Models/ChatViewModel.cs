using Penguin.Cms.Communication;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Penguin.Cms.Modules.Communication.Models
{
    public class ChatViewModel
    {
        public string LastMessage => this.Messages.LastOrDefault()?.Contents ?? string.Empty;

        public DateTime LastMessageTime => this.Messages.LastOrDefault()?.DateCreated ?? this.Session.DateCreated;

        public List<ChatMessage> Messages { get; } = new List<ChatMessage>();

        public ChatSession Session { get; set; }

        public IEnumerable<string> Users => this.Messages.Select(m => m.DisplayName).Distinct();

        public ChatViewModel(ChatSession session)
        {
            Session = session;
        }
    }
}