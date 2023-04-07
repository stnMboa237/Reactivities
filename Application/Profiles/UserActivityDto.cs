using System.Text.Json.Serialization;

namespace Application.Profiles
{
    public class UserActivityDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public DateTime Date { get; set; }
        
        /*this prop will not returned to the client*/
        [JsonIgnore]
        public string HostUsername { get; set; }
    }
}