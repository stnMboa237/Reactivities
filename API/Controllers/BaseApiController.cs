using API.Extensions;
using Application.Core;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BaseApiController : ControllerBase
    {
        private IMediator _mediator;

        // Mediator = _mediator. 
        // Si _mediator = null; alors, Mediator = HttpContext.RequestServices.GetService<IMediator>() du controller oú arrive la requête.
        protected IMediator Mediator => _mediator ??= HttpContext.RequestServices.GetService<IMediator>();
        
        protected ActionResult HandleResult<T>(Result<T> result)
        {

            if (result == null) return NotFound();

            if (result.IsSuccess && result.Value != null)
                return Ok(result.Value);

            if (result.IsSuccess && result.Value == null)
                return NotFound();

            return BadRequest(result.Error);
        }

        /*we will return a paged list for every queryHandler which initially returned list of items*/
        protected ActionResult HandlePagedResult<T>(Result<PagedList<T>> result)
        {

            if (result == null) return NotFound();

            if (result.IsSuccess && result.Value != null)
            { 
                Response.AddPaginationHeader(result.Value.CurrentPage, result.Value.PageSize, 
                    result.Value.TotalCount, result.Value.TotalPages);
                return Ok(result.Value); 
            }

            if (result.IsSuccess && result.Value == null)
                return NotFound();

            return BadRequest(result.Error);
        }
    }
}