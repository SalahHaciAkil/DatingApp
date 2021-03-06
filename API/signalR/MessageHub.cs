using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.interfaces;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;

namespace API.signalR
{
    public class MessageHub : Hub
    {
        private readonly IMapper mapper;
        private readonly IUnitOfWork unitOfWork;
        private readonly PresenceTracker presenceTracker;
        private readonly IHubContext<PresenceHub> presenceHub;

        public MessageHub(IMapper mapper, IUnitOfWork unitOfWork,
         PresenceTracker presenceTracker, IHubContext<PresenceHub> presenceHub)
        {
            this.presenceTracker = presenceTracker;
            this.presenceHub = presenceHub;
            this.mapper = mapper;
            this.unitOfWork = unitOfWork;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            string otherUser = httpContext.Request.Query["user"].ToString();
            string userName = Context.User.getUserName();
            var groupName = GetGroupName(userName, otherUser);
            await AddToGroup(groupName);

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            var messages = await this.unitOfWork.messageRepo.GetMessagesThreadAsync(userName, otherUser);

            if (this.unitOfWork.HasChanges())
                await this.unitOfWork.Complete();

            await Clients.Groups(groupName).SendAsync("ReceiveMessageThread", messages);




        }

        private async Task<bool> AddToGroup(string groupName)
        {
            var group = await this.unitOfWork.messageRepo.GetMessageGroup(groupName);
            var connection = new Connection(Context.ConnectionId, Context.User.getUserName());
            if (group == null)
            {
                group = new Group(groupName);
                this.unitOfWork.messageRepo.AddGroup(group);
            }


            group.Connections.Add(connection);

            return (await this.unitOfWork.Complete());
        }


        private async Task<bool> RemoveConnection()
        {
            var connection = await this.unitOfWork.messageRepo.GetConnection(Context.ConnectionId);
            this.unitOfWork.messageRepo.RemoveConnection(connection);

            return (await this.unitOfWork.Complete());
        }

        private string GetGroupName(string userName, string otherUser)
        {
            var stringCompareResult = string.CompareOrdinal(userName, otherUser) > 0;
            return stringCompareResult ? $"{userName}-{otherUser}" : $"{otherUser}-{userName}";

        }

        public async Task SendMessage(CreateMessageDto createMessageDto)
        {
            var senderUserName = Context.User.getUserName();


            var sender = await this.unitOfWork.UserRepo.GetUserAsync(senderUserName);
            var receptient = await this.unitOfWork.UserRepo.GetUserAsync(createMessageDto.ReciptientUserName);

            if (receptient == null)
                throw new HubException("No user with name " + createMessageDto.ReciptientUserName);
            if (senderUserName == createMessageDto.ReciptientUserName.ToLower())
                throw new HubException("You cannot send to yourself");

            var recepientUserName = createMessageDto.ReciptientUserName;


            var message = new Message
            {
                Content = createMessageDto.Content,
                SenderUserName = senderUserName,
                Sender = sender,
                Recipient = receptient,
                ReciptientUserName = recepientUserName,

            };
            var groupName = GetGroupName(senderUserName, createMessageDto.ReciptientUserName);
            var group = await this.unitOfWork.messageRepo.GetMessageGroup(groupName);

            if (group.Connections.Any(x => x.UserName == recepientUserName))
                message.DateRead = DateTime.UtcNow;

            else
            {
                var connections = await this.presenceTracker.GetUserConnectionIds(recepientUserName);
                if (connections != null)
                {
                    await this.presenceHub.Clients.Clients(connections)
                    .SendAsync("NewMessageReceived", new { userName = senderUserName, knowAs = sender.KnownAs });
                }

            }

            this.unitOfWork.messageRepo.AddMessage(message);
            if (await this.unitOfWork.Complete())
            {
                var groupp = Clients.Groups(groupName);

                await groupp.SendAsync("NewMessage", this.mapper.Map<MessageDto>(message));
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await RemoveConnection();
            await base.OnDisconnectedAsync(exception);
        }


    }
}