using Application.Activities;
using Application.Comments;
using AutoMapper;
using Domain;

namespace Application.Core
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles()
        {
            string currentUsername = null;
            CreateMap<Activity, Activity>();

            CreateMap<Activity, ActivityDto>()
                .ForMember(d => d.HostUsername, o => o.MapFrom(
                    s => s.Attendees.FirstOrDefault(x => x.IsHost).AppUser.UserName
                ));
            
            CreateMap<ActivityAttendee, AttendeeDto>()
                .ForMember(d => d.DisplayName, o => o.MapFrom(x => x.AppUser.DisplayName))
                .ForMember(d => d.Username, o => o.MapFrom(x => x.AppUser.UserName))
                .ForMember(d => d.Bio, o => o.MapFrom(x => x.AppUser.Bio))
                .ForMember(d =>d.Image, o => o.MapFrom(s => s.AppUser.Photos.FirstOrDefault(x => x.IsMain).Url))
                .ForMember(d => d.FollowersCount, o => o.MapFrom(s => s.AppUser.Followers.Count))
                .ForMember(d => d.FollowingCount, o => o.MapFrom(s => s.AppUser.Followings.Count))
                .ForMember(d => d.Following, o => o.MapFrom(s => s.AppUser.Followers.Any(x => x.Observer.UserName == currentUsername)));
            
            CreateMap<AppUser, Profiles.Profile>()
                .ForMember(d =>d.Image, o => o.MapFrom(s => s.Photos.FirstOrDefault(x => x.IsMain).Url))
                .ForMember(d => d.FollowersCount, o => o.MapFrom(s => s.Followers.Count))
                .ForMember(d => d.FollowingCount, o => o.MapFrom(s => s.Followings.Count))
                .ForMember(d => d.Following, o => o.MapFrom(s => s.Followers.Any(x => x.Observer.UserName == currentUsername)));

            CreateMap<Comment, CommentDto>()
                .ForMember(d => d.DisplayName, o => o.MapFrom(x => x.Author.DisplayName))
                .ForMember(d => d.Username, o => o.MapFrom(x => x.Author.UserName))
                .ForMember(d =>d.Image, o => o.MapFrom(s => s.Author.Photos.FirstOrDefault(x => x.IsMain).Url));

            CreateMap<ActivityAttendee, Profiles.UserActivityDto>()
                .ForMember(d => d.Id, o => o.MapFrom(x => x.Activity.Id))
                .ForMember(d => d.Title, o => o.MapFrom(x => x.Activity.Title))
                .ForMember(d => d.Category, o => o.MapFrom(x => x.Activity.Category))
                .ForMember(d => d.Date, o => o.MapFrom(x => x.Activity.Date))
                .ForMember(d => d.HostUsername, o => o.MapFrom(x => x.Activity.Attendees.FirstOrDefault(a => a.IsHost).AppUser.UserName));
        }
    }
}