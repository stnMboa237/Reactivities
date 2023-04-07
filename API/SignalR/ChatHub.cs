using Application.Comments;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    public class ChatHub : Hub
    {
        private readonly IMediator _mediator;
        public ChatHub(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task SendComment(Create.Command cmd)
        {
            // save the comment to the DB
            var comment = await _mediator.Send(cmd);
            // send the comment to all connected users members of a group
            await Clients.Group(cmd.ActivityId.ToString())
                .SendAsync("ReceiveComment", comment.Value); /*ReceiveComment is the name of a method which returns the 
                comment sent by the user previously to all connected user (user from the same group) */
        }

        /*when a client connects to the HUB. we want him to join a group, OnConnectedAsync is called when a client is connecting to the HUB*/
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            /*get the value of key("activityId") from the request. key value MUST match with the name into the request*/
            var activityId = httpContext.Request.Query["activityId"];
            // add the user to a group named activityId.toString()
            await Groups.AddToGroupAsync(Context.ConnectionId, activityId);

            //get the list of comments
            var result = await _mediator.Send(new List.Query { ActivityId = Guid.Parse(activityId) });

            //send the list of comments to all connected User memeber of the group named activityId
            await Clients.Caller.SendAsync("LoadComments", result.Value);
        }
    }
}