using Hika.Api.Common;
using Hika.Application.Chat;
using Hika.Application.Chat.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/bookings/{bookingId:guid}/conversation")]
[Authorize]
public sealed class ChatController(IChatService chatService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ConversationResponse>> GetConversation(Guid bookingId, CancellationToken cancellationToken)
    {
        var conversation = await chatService.GetAsync(User.GetUserId(), bookingId, cancellationToken);
        return Ok(conversation);
    }

    [HttpPost("messages")]
    public async Task<ActionResult<ChatMessageResponse>> SendMessage(
        Guid bookingId, SendChatMessageRequest request, CancellationToken cancellationToken)
    {
        var message = await chatService.SendMessageAsync(User.GetUserId(), bookingId, request.Message, cancellationToken);
        return Ok(message);
    }
}
