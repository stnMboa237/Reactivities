using Application.Core;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Profiles
{
    public class ListUserActivity
    {
        public class Query : IRequest<Result<List<UserActivityDto>>>
        {
            public string Predicate { get; set; }
            public string Username { get; set; }
        }

        public class Handler : IRequestHandler<Query, Result<List<UserActivityDto>>>
        {
            private readonly DataContext _context;
            private readonly IMapper _mapper;
            public Handler(DataContext context, IMapper mapper)
            {
                _mapper = mapper;
                _context = context;
            }
            public async Task<Result<List<UserActivityDto>>> Handle(Query request, CancellationToken cancellationToken)
            {
                if (request.Predicate == null || request.Username == null) return null;
                var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName == request.Username);
                if (user == null) return null;

                var userActivities = new List<UserActivityDto>();

                var query = _context.ActivityAttendees
                    .Where(x => x.AppUser.UserName == request.Username)
                    .OrderBy(x => x.Activity.Date)
                    .ProjectTo<UserActivityDto>(_mapper.ConfigurationProvider)
                    .AsQueryable();

                switch(request.Predicate) {
                    case "past": 
                        query = query.Where(x => x.Date < DateTime.UtcNow); 
                        break;
                    case "hosting": 
                        query = query.Where(x => x.HostUsername == request.Username);
                        break;
                    case "future":
                        query = query.Where(x => x.Date >= DateTime.UtcNow);
                        break;
                }
                userActivities = await query.ToListAsync();
                return Result<List<UserActivityDto>>.Success(userActivities);
            }
        }

    }
}