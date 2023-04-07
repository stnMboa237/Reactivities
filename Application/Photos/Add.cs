using Application.Core;
using Application.Interfaces;
using Domain;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Photos
{
    public class Add
    {
        public class Command : IRequest<Result<Photo>>
        {
            public IFormFile File { get; set; }
        }

        public class Handler : IRequestHandler<Command, Result<Photo>>
        {
            private readonly IUserAccessor _userAccessor;
            private readonly IPhotoAccessor _photoAccessor;
            private readonly DataContext _context;
            public Handler(DataContext context, IPhotoAccessor photoAccessor, IUserAccessor userAccessor)
            {
                _context = context;
                _photoAccessor = photoAccessor;
                _userAccessor = userAccessor;
            }

            public async Task<Result<Photo>> Handle(Command request, CancellationToken cancellationToken)
            {
                var user = await _context.Users
                    .Include(p => p.Photos)
                    .FirstOrDefaultAsync(x => x.UserName == _userAccessor.GetUserName());

                if (user == null) return null;

                //cette ligne lancera une exception en cas de souci dans AddPhoto
                var photoUploadResult = await _photoAccessor.AddPhoto(request.File);

                var photo = new Photo {
                    Url = photoUploadResult.Url,
                    Id = photoUploadResult.PublicId
                };

                //si l'user courant n'a pas de photo Main (principale), alors, la photo uploadé deviendra la principale
                if (!user.Photos.Any(p => p.IsMain)) {
                    photo.IsMain = true;
                }

                user.Photos.Add(photo);

                var result = await _context.SaveChangesAsync() > 0;
                
                return result ? Result<Photo>.Success(photo) : Result<Photo>.Failure("Problem adding photo");
            }
        }
    }
}