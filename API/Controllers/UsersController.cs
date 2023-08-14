using System.Security.Claims;
using API.Data;
using API.DTO;
using API.Entities;
using API.Extensions;
using API.Helpers;
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
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;
        private readonly IUnitOfWork _uow;

        public UsersController(IMapper mapper, IPhotoService photoService, IUnitOfWork uow)
        {
            this._uow = uow;
            this._mapper = mapper;
            this._photoService = photoService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedList<MemberDto>>> GetUsers([FromQuery]UserParams userParams) {
           var gender = await this._uow.UserRepository.GetUserGender(User.GetUserName());
           userParams.CurrentUsername = User.GetUserName();

           if (string.IsNullOrEmpty(userParams.Gender)) {
                userParams.Gender = (gender == "male") ? "female" : "male";
           }

            var users = await this._uow.UserRepository.GetMembersAsync(userParams);

            Response.AddPaginationHeader(new PaginationHeader(users.CurrentPage, users.PageSize, 
                users.TotalCount, users.TotalPages));

            return Ok(users);
        }

        [HttpGet("{username}")]
        public async Task<ActionResult<MemberDto>> GetUser(string username) {
            // var user = await this._uow.UserRepository.GetUserByUsernameAsync(username);

            // return this._mapper.Map<MemberDto>(user);

            return await this._uow.UserRepository.GetMemberAsync(username);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto) {
            var user = await this._uow.UserRepository.GetUserByUsernameAsync(User.GetUserName());

            if (user == null) {
                return NotFound();
            }

            _mapper.Map(memberUpdateDto, user);

            if (await _uow.Complete()) {
                return NoContent();
            }
            return BadRequest("Failed to update user");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file) {
            var user = await this._uow.UserRepository.GetUserByUsernameAsync(User.GetUserName());

            if (user == null) {
                return NotFound();
            }

            var result = await this._photoService.AddPhotoAsync(file);

            if (result.Error != null) {
                return BadRequest(result.Error.Message);
            }

            var photo = new Photo {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };

            if (user.Photos.Count == 0) {
                photo.IsMain = true;
            }
            user.Photos.Add(photo);

            if (await this._uow.Complete()) {
                return CreatedAtAction(nameof(GetUser), new {username = user.UserName}, 
                    this._mapper.Map<PhotoDto>(photo));
                // return this._mapper.Map<PhotoDto>(photo);
            }

            return BadRequest("Problems adding photos");
        }

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId) {
            var user = await this._uow.UserRepository.GetUserByUsernameAsync(User.GetUserName());

            if (user == null) {
                return NotFound("user is missing");
            }

            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);
            if (photo == null) {
                return NotFound();
            }
            if (photo.IsMain) {
                return BadRequest("This is already your main photo");
            }

            var mainPhoto = user.Photos.FirstOrDefault(x => x.IsMain);
            if (mainPhoto != null) {
                mainPhoto.IsMain = false;
            }
            photo.IsMain = true;

            if (await this._uow.Complete()) {
                return NoContent();
            }
            return BadRequest("Error happened while setting main photo");
        } 

        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId) {
            var user = await this._uow.UserRepository.GetUserByUsernameAsync(User.GetUserName());

             if (user == null) {
                return NotFound("user is missing");
            }

            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);
            if (photo == null) {
                return NotFound();
            }
            if (photo.IsMain) {
                return BadRequest("You can't delete your main photo");
            }

            if (photo.PublicId != null) {
                var result = await _photoService.DeletePhotoAsync(photo.PublicId);
                if (result.Error != null) {
                    return BadRequest(result.Error.Message);
                }
            }
            user.Photos.Remove(photo);
            if (await this._uow.Complete()) {
                return Ok();
            }
            return BadRequest("Problem deleting photo");
        }
    }
}