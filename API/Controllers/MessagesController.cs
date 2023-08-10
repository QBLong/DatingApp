using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTO;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class MessagesController: BaseApiController
    {
        private readonly IUserRepository _userRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IMapper _mapper;

        public MessagesController(IUserRepository userRepository, 
            IMessageRepository messageRepository, IMapper mapper)
        {
            this._userRepository = userRepository;
            this._messageRepository = messageRepository;
            this._mapper = mapper;
        }

        [HttpPost]
        public async Task<ActionResult<MessageDto>> CreateMessage(CreateMessageDto createMessageDto) {
            var username = User.GetUserName();

            if (username == createMessageDto.RecipientUsername.ToLower()) 
            {
                return BadRequest("You can't send messages to yourself");
            }

            var sender = await this._userRepository.GetUserByUsernameAsync(username);
            var recipient = await this._userRepository.GetUserByUsernameAsync(createMessageDto.RecipientUsername);

            if (recipient == null) return NotFound();

            var message = new Message {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.UserName,
                RecipientUsername = recipient.UserName,
                Content = createMessageDto.Content
            };

            this._messageRepository.AddMessage(message);
            if (await this._messageRepository.SaveAllAsync()) {
                return Ok(this._mapper.Map<MessageDto>(message));
            }
            return BadRequest("Failed to send messages");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessagesForUser([FromQuery]
            MessageParams messageParams)
        {
            messageParams.Username = User.GetUserName();

            var messages = await _messageRepository.GetMessagesForUser(messageParams);

            Response.AddPaginationHeader(new PaginationHeader(messages.CurrentPage,
                messages.PageSize, messages.TotalCount, messages.TotalPages));

            return messages;
        }

        [HttpGet("thread/{username}")]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessageThread(string username) {
            var currentUserName = User.GetUserName();

            return Ok(await this._messageRepository.GetMessageThread(currentUserName, username));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage(int id) {
            var username = User.GetUserName();
            var message = await this._messageRepository.GetMessage(id);

            if (message.SenderUsername != username && message.RecipientUsername != username) {
                return Unauthorized();
            }

            if (message.SenderUsername == username) {
                message.SenderDeleted = true;
            }

            if (message.RecipientUsername == username) {
                message.RecipientDeleted = true;
            }

            if (message.SenderDeleted && message.RecipientDeleted) {
                this._messageRepository.DeleteMessage(message);
            }
            if (await this._messageRepository.SaveAllAsync()) return Ok();
            
            return BadRequest("Failed to delete the message");
        }
    }
}