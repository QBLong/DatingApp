using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Threading.Tasks;
using API.DTO;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    [Authorize]
    public class MessageHub: Hub
    {
        private readonly IMapper _mapper;
        private readonly IHubContext<PresenceHub> _presenceHub;
        private readonly IUnitOfWork _uow;

        public MessageHub(IMapper mapper, IHubContext<PresenceHub> hubContext, IUnitOfWork uow)
        {
            this._uow = uow;
            this._presenceHub = hubContext;
            this._mapper = mapper;        
        }

        public override async Task OnConnectedAsync() {
            var httpContext = Context.GetHttpContext();
            var otherUser = httpContext.Request.Query["user"];
            var groupName = GetGroupName(Context.User.GetUserName(), otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            var group = await AddToGroup(groupName);

            await Clients.Group(groupName).SendAsync("UpdatedGroup", group);

            var messages = await _uow.MessageRepository.GetMessageThread(Context.User.GetUserName(), otherUser);

            if (_uow.HasChanges()) {
                await _uow.Complete();
            }

            await Clients.Caller.SendAsync("ReceiveMessageThread", messages);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var group = await RemoveFromMessageGroup();
                        
            await Clients.Group(group.Name).SendAsync("UpdatedGroup");

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(CreateMessageDto createMessageDto) {
            var username = Context.User.GetUserName();

            if (username == createMessageDto.RecipientUsername.ToLower()) 
            {
                throw new HubException("You can't send messages to yourself");
            }

            var sender = await this._uow.UserRepository.GetUserByUsernameAsync(username);
            var recipient = await this._uow.UserRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername);

            if (recipient == null) throw new HubException("Not found user");

            var message = new Message {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.UserName,
                RecipientUsername = recipient.UserName,
                Content = createMessageDto.Content
            };

            var groupName = GetGroupName(sender.UserName, recipient.UserName);
            var group = await _uow.MessageRepository.GetMessageGroup(groupName);

            if (group.Connections.Any(x => x.Username == recipient.UserName)) {
                message.DateRead = DateTime.UtcNow;
            }
            else {
                var connections = await PresenceTracker.GetConnectionsForUser(recipient.UserName);
                if (connections != null) {
                    await _presenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived", 
                        new {username = sender.UserName, KnownAs = sender.KnownAs});
                }
            }

            this._uow.MessageRepository.AddMessage(message);
            if (await this._uow.Complete()) {
                await Clients.Group(groupName).SendAsync("NewMessage", this._mapper.Map<MessageDto>(message));
            }
        }

        private string GetGroupName(string caller, string otherUser) {
            var stringCompare = string.CompareOrdinal(caller, otherUser) < 0;
            return stringCompare ? $"{caller}-{otherUser}" : $"{otherUser}-{caller}";
        }

        private async Task<Group> AddToGroup(string groupName) {
            var group = await _uow.MessageRepository.GetMessageGroup(groupName);
            var connection = new Connection(Context.ConnectionId, Context.User.GetUserName());

            if (group == null) {
                group = new Group(groupName);
                _uow.MessageRepository.AddGroup(group);
            }

            group.Connections.Add(connection);
            if (await _uow.Complete()) {
                return group;
            }
            throw new HubException("Failed to add connection to group");
        }

        private async Task<Group> RemoveFromMessageGroup() {
            var group = await _uow.MessageRepository.GetGroupForConnection(Context.ConnectionId);
            var connection = group.Connections.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            _uow.MessageRepository.RemoveConnection(connection);

            if (await _uow.Complete()) {
                return group;
            }
            throw new HubException("Failed to remove connection from hub");
        }
    }
}