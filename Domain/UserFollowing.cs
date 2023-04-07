namespace Domain
{
    public class UserFollowing
    {
        public string ObserverId { get; set; }
        //the observer> the person who is going to follow an another user
        public AppUser Observer { get; set; }
        public string TargetId { get; set; }
        public AppUser Target { get; set; }
    }
}