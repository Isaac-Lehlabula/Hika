using FluentValidation;
using Hika.Application.Chat.Dtos;
using Hika.Domain.Chat;

namespace Hika.Application.Chat.Validators;

public sealed class SendChatMessageRequestValidator : AbstractValidator<SendChatMessageRequest>
{
    public SendChatMessageRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(ChatMessage.MaxBodyLength);
    }
}
