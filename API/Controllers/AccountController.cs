using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTO;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext dataContext;
        private readonly ITokenService tokenService;
        private readonly IUserRepository userRepository;
        private readonly IMapper _mapper;

        public AccountController(DataContext dataContext, ITokenService tokenService, 
            IUserRepository userRepository, IMapper mapper)
        {
            this.dataContext = dataContext;
            this.tokenService = tokenService;
            this.userRepository = userRepository;
            this._mapper = mapper;
        }

        [HttpPost("register")] //api/account/register
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto) {
            if (await CheckUserExist(registerDto.Username)) {
                return BadRequest("This username has been taken by someone else");
            }
            var user = this._mapper.Map<AppUser>(registerDto);
            using (var hmac = new HMACSHA512()) {
                user.UserName = registerDto.Username.ToLower();
                user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password));
                user.PasswordSalt = hmac.Key;

                dataContext.Users.Add(user);
                await dataContext.SaveChangesAsync();

                return new UserDto {
                    Username = user.UserName,
                    Token = tokenService.CreateToken(user),
                    KnownAs = user.KnownAs
                };
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto) {
            var user = await dataContext.Users
                .Include(x => x.Photos)
                .FirstOrDefaultAsync(x => x.UserName == loginDto.Username.ToLower());
            // var user = await this.userRepository.GetUserByUsernameAsync(loginDto.Username.ToLower());

            if (user == null) {
                return Unauthorized("This username is not valid");
            }

            using (var hmac = new HMACSHA512(user.PasswordSalt)) {
                var PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

                for (int i = 0; i < PasswordHash.Count(); i++) {
                    if (PasswordHash[i] != user.PasswordHash[i]) {
                        return Unauthorized("This password is invalid");
                    }
                }

                return new UserDto {
                    Username = user.UserName,
                    Token = tokenService.CreateToken(user),
                    PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain).Url,
                    KnownAs = user.KnownAs
                };
            }
        }

        private async Task<bool> CheckUserExist(string username) {
            return await dataContext.Users.AnyAsync(x => x.UserName == username.ToLower());
        }
    }
}