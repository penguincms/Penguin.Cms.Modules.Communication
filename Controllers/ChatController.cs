using Microsoft.AspNetCore.Mvc;
using Penguin.Cms.Communication;
using Penguin.Cms.Communication.Repositories;
using Penguin.Cms.Modules.Communication.Models;
using Penguin.Persistence.Abstractions.Interfaces;
using Penguin.Security.Abstractions.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Penguin.Cms.Modules.Communication.Controllers
{
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
            List<ChatViewModel> AllChats = new();

            foreach (ChatSession thisSession in ChatSessionRepository.GetOpenChatsForUser())
            {
                ChatViewModel chatViewModel = new(thisSession);

                chatViewModel.Messages.AddRange(ChatMessageRepository.GetMessages(thisSession.Guid));

                if (chatViewModel.Messages.Any())
                {
                    AllChats.Add(chatViewModel);
                }
            }

            return View(AllChats);
        }

        public ActionResult Open(string Id)
        {
            ChatSession existing = ChatSessionRepository.GetForUsers(UserSession.LoggedInUser.Guid, Guid.Parse(Id)) ?? throw new NullReferenceException($"Can not find ChatSession with Id {Id}");

            if (existing is null)
            {
                string cId = string.Empty;

                using (IWriteContext context = ChatSessionRepository.WriteContext())
                {
                    cId = ChatSessionRepository.OpenSession(UserSession.LoggedInUser.Guid, Guid.Parse(Id)).Guid.ToString();
                }

                return RedirectToAction(nameof(ViewChat), new { Id = cId, area = "" });
            }
            else
            {
                return RedirectToAction(nameof(ViewChat), new { Id = existing.Guid, area = "" });
            }
        }

        [HttpPost]
        public virtual ActionResult SendMessage(Guid Id, string Contents)
        {
            ChatMessage sent;

            using (IWriteContext writeContext = ChatMessageRepository.WriteContext())
            {
                ChatSession target = ChatSessionRepository.Find(Id);

                if (target is null)
                {
                    throw new UnauthorizedAccessException();
                }

                if (!string.IsNullOrWhiteSpace(Contents))
                {
                    sent = ChatMessageRepository.SendMessageToChat(target, UserSession.LoggedInUser, Contents);
                    return View("ViewMessage", sent);
                }
            }

            throw new Exception("An error occurred sending the message");
        }

        public ActionResult Update(string Id)
        {
            return Id is null
                ? throw new ArgumentNullException(nameof(Id))
                : Id.Contains('-', StringComparison.Ordinal)
                ? View("ViewMessages", ChatMessageRepository.GetMessagesFor(Guid.Parse(Id)))
                : (ActionResult)View("ViewMessages", ChatMessageRepository.GetMessagesAfter(int.Parse(Id, NumberStyles.Integer, CultureInfo.CurrentCulture)));
        }

        public ActionResult ViewChat(string Id)
        {
            ChatViewModel chatViewModel = new(ChatSessionRepository.Find(Guid.Parse(Id)));

            chatViewModel.Messages.AddRange(ChatMessageRepository.GetMessages(chatViewModel.Session.Guid));

            return View(chatViewModel);
        }
    }
}