using Hika.Api.Common;
using Hika.Application.Reviews;
using Hika.Application.Reviews.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/reviews")]
public sealed class ReviewsController(IReviewService reviewService) : ControllerBase
{
    [HttpPost("bookings/{bookingId:guid}")]
    [Authorize]
    public async Task<ActionResult<ReviewResponse>> SubmitReview(
        Guid bookingId, CreateReviewRequest request, CancellationToken cancellationToken)
    {
        var review = await reviewService.SubmitAsync(User.GetUserId(), bookingId, request, cancellationToken);
        return Ok(review);
    }

    [HttpGet("users/{userId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReviewsForUser(
        Guid userId, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var result = await reviewService.GetForUserAsync(userId, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
