using System.Security.Claims;
using API.Data;
using API.DTO;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    // [ApiController]
    // [Route("api/[controller]")] // /api/users
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public UsersController(IUserRepository userRepository, IMapper mapper)
        {
            this._userRepository = userRepository;
            this._mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers() {
            // var users = await this._userRepository.GetUsersAsync();

            // var usersToReturn = this._mapper.Map<IEnumerable<MemberDto>>(users);

            // return Ok(usersToReturn);

            var users = await this._userRepository.GetMembersAsync();

            return Ok(users);
        }

        [HttpGet("{username}")]
        public async Task<ActionResult<MemberDto>> GetUser(string username) {
            // var user = await this._userRepository.GetUserByUsernameAsync(username);

            // return this._mapper.Map<MemberDto>(user);

            return await this._userRepository.GetMemberAsync(username);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto) {
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await this._userRepository.GetUserByUsernameAsync(username);

            if (user == null) {
                return NotFound();
            }

            _mapper.Map(memberUpdateDto, user);

            if (await _userRepository.SaveAllAsync()) {
                return NoContent();
            }
            return BadRequest("Failed to update user");
        }
    }
}