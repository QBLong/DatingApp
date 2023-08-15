using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTO;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class PhotoRepository : IPhotoRepository
    {
        private readonly IMapper _mapper;
        private readonly DataContext _context;
        public PhotoRepository(DataContext context, IMapper mapper)
        {
            this._context = context;
            this._mapper = mapper;
            
        }
        public async Task<Photo> GetPhotoById(int photoId)
        {
            return await _context.Photos
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(x => x.Id == photoId);
        }

        public async Task<IEnumerable<PhotoForApprovalDto>> GetUnapprovedPhotos()
        {
            return await _context.Photos
                .IgnoreQueryFilters()
                .Where(x => !x.IsApproved).Select(x => new PhotoForApprovalDto {
                    Id = x.Id,
                    UserName = x.AppUser.UserName,
                    Url = x.Url,
                    IsApproved = x.IsApproved
                })
                .ToListAsync();
        }

        public void RemovePhoto(Photo photo)
        {
            _context.Photos.Remove(photo);
        }
    }
}