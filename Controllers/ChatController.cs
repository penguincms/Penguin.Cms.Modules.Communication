using Microsoft.AspNetCore.Mvc;
using Penguin.Cms.Communication;
using Penguin.Cms.Modules.Communication.Models;
using Penguin.Cms.Modules.Communication.Repositories;
using Penguin.Persistence.Abstractions.Interfaces;
using Penguin.Security.Abstractions.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace Penguin.Cms.Modules.Communication.Controllers
{
    [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
    public class ChatController : Controller
    {
        protected ChatMessageRepository ChatMessageRepository { get; set; }

        protected ChatSessionRepository ChatSessionRepository { get; set; }

        protected ChatUserSessionRepository ChatUserSessionRepository { get; set; }

        protected IUserSession UserSession { get; set; }

        public ChatController(ChatUserSessionRepository chatUserSessionRepository, ChatSessionRepository chatSessionRepository, ChatMessageRepository chatMessageRepository, IUserSession userSession)
        {
            ChatMessageRepository = chatMessageRepository;
            ChatSessionRepository = chatSessionRepository;
            ChatUserSessionRepository = chatUserSessionRepository;
            UserSession = userSession;
        }

        public ActionResult All()
        {
            List<ChatViewModel> AllChats = new List<ChatViewModel>();

            foreach (ChatSession thisSession in this.ChatSessionRepository.GetOpenChatsForUser())
            {
                ChatViewModel chatViewModel = new ChatViewModel(thisSession);

                chatViewModel.Messages.AddRange(this.ChatMessageRepository.GetMessages(thisSession.Guid));

                if (chatViewModel.Messages.Any())
                {
                    AllChats.Add(chatViewModel);
                }
            }

            return this.View(AllChats);
        }

        public ActionResult Open(string Id)
        {
            ChatSession existing = this.ChatSessionRepository.GetForUsers(this.UserSession.LoggedInUser.Guid, Guid.Parse(Id)) ?? throw new NullReferenceException($"Can not find ChatSession with Id {Id}");

            if (existing is null)
            {
                string cId = string.Empty;

                using (IWriteContext context = this.ChatSessionRepository.WriteContext())
                {
                    cId = this.ChatSessionRepository.OpenSession(this.UserSession.LoggedInUser.Guid, Guid.Parse(Id)).Guid.ToString();
                }

                return this.RedirectToAction(nameof(ViewChat), new { Id = cId, area = "" });
            }
            else
            {
                return this.RedirectToAction(nameof(ViewChat), new { Id = existing.Guid, area = "" });
            }
        }

        [HttpPost]
        public virtual ActionResult SendMessage(Guid Id, string Contents)
        {
            ChatMessage sent;

            using (IWriteContext writeContext = ChatMessageRepository.WriteContext())
            {
                ChatSession target = this.ChatSessionRepository.Find(Id);

                if (target is null)
                {
                    throw new UnauthorizedAccessException();
                }

                if (!string.IsNullOrWhiteSpace(Contents))
                {
                    sent = this.ChatMessageRepository.SendMessageToChat(target, this.UserSession.LoggedInUser, Contents);
                    return this.View("ViewMessage", sent);
                }
            }

            throw new Exception("An error occurred sending the message");
        }

        public ActionResult Update(string Id)
        {
            if (Id is null)
            {
                throw new ArgumentNullException(nameof(Id));
            }

            if (Id.Contains('-', StringComparison.Ordinal))
            {
                return this.View("ViewMessages", this.ChatMessageRepository.GetMessagesFor(Guid.Parse(Id)));
            }
            else
            {
                return this.View("ViewMessages", this.ChatMessageRepository.GetMessagesAfter(int.Parse(Id, NumberStyles.Integer, CultureInfo.CurrentCulture)));
            }
        }

        public ActionResult ViewChat(string Id)
        {
            ChatViewModel chatViewModel = new ChatViewModel(this.ChatSessionRepository.Find(Guid.Parse(Id)));

            chatViewModel.Messages.AddRange(this.ChatMessageRepository.GetMessages(chatViewModel.Session.Guid));

            return this.View(chatViewModel);
        }
    }
}