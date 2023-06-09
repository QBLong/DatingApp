using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTO;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class UserRepository : IUserRepository
    {
        public readonly DataContext _context;
        public readonly IMapper _mapper;
        public UserRepository(DataContext context, IMapper mapper)
        {
            this._mapper = mapper;
            this._context = context;
            
        }
        public async Task<AppUser> GetUserByIdAsync(int id)
        {
            return await this._context.Users.FindAsync(id);
        }

        public async Task<AppUser> GetUserByUsernameAsync(string username)
        {
            return await this._context.Users
            .Include(p => p.Photos)
            .SingleOrDefaultAsync(x => x.UserName == username);
        }

        public async Task<IEnumerable<AppUser>> GetUsersAsync()
        {
            return await this._context.Users
            .Include(p => p.Photos)
            .ToListAsync();
        }

        public async Task<bool> SaveAllAsync()
        {
            return await this._context.SaveChangesAsync() > 0;
        }

        public void Update(AppUser user)
        {
            this._context.Entry(user).State = EntityState.Modified;
        }

        public async Task<IEnumerable<MemberDto>> GetMembersAsync()
        {
            return await this._context.Users
                .ProjectTo<MemberDto>(this._mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<MemberDto> GetMemberAsync(string username)
        {
            return await this._context.Users
                .Where(user => user.UserName == username)
                .ProjectTo<MemberDto>(this._mapper.ConfigurationProvider)
                .SingleOrDefaultAsync();
        }
    }
}