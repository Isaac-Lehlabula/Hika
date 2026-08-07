using Hika.Api.Common;
using Hika.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/admin/reviews")]
[Authorize(Policy = "Admin")]
public sealed class AdminReviewsController(IAdminReviewService adminReviewService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetReviews([FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var result = await adminReviewService.GetReviewsAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{reviewId:guid}")]
    public async Task<IActionResult> DeleteReview(Guid reviewId, CancellationToken cancellationToken)
    {
        await adminReviewService.DeleteAsync(User.GetUserId(), reviewId, cancellationToken);
        return NoContent();
    }
}
