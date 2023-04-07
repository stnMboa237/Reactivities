using Application.Profiles;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class ProfilesController: BaseApiController
    {
        [HttpGet("{username}")]
        public async Task<IActionResult> GetProfile(string username) {
            return HandleResult(await Mediator.Send(new Application.Profiles.Details.Query {Username = username}));
        }

        [HttpPut]
        public async Task<IActionResult> EditProfile(Edit.Command command) {
            return HandleResult(await Mediator.Send(new Application.Profiles.Edit.Command {
                DisplayName = command.DisplayName,
                Bio = command.Bio
            }));
        }

        [HttpGet("{username}/activities")]
        public async Task<IActionResult> GetUserActivities(string username, string predicate) {
            return HandleResult(await Mediator.Send(new Application.Profiles.ListUserActivity.Query {
                Username = username,
                Predicate = predicate
            }));
        }
    }
}