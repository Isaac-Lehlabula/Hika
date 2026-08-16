using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

/// <summary>
/// The pages Ozow's hosted payment page redirects the passenger's browser to after they finish
/// (or abandon) paying — see OzowPaymentGateway's SuccessUrl/CancelUrl/ErrorUrl. These are
/// plain, unauthenticated static pages: the passenger's browser has no session with us (they
/// were never logged into this page, just redirected through it), and the actual booking/payment
/// state change already happened server-to-server via OzowWebhooksController by the time — or
/// shortly after — this page loads. This page only tells the passenger what happened and to
/// switch back to the Hiking Spot app, where BookingDetailScreen refreshes on resume.
/// </summary>
[ApiController]
[Route("payment-return")]
[AllowAnonymous]
public sealed class PaymentReturnController : ControllerBase
{
    [HttpGet("success")]
    public ContentResult Success() => Page(
        title: "Payment received",
        tone: "success",
        icon: "&#10003;",
        heading: "You're all set",
        message: "Your payment went through and your seats are confirmed. You can close this page and return to the Hiking Spot app.");

    [HttpGet("cancel")]
    public ContentResult Cancel() => Page(
        title: "Payment cancelled",
        tone: "neutral",
        icon: "&#10005;",
        heading: "Payment cancelled",
        message: "You cancelled the payment, so your seats haven't been confirmed yet. Return to the Hiking Spot app to try again.");

    [HttpGet("error")]
    public ContentResult Error() => Page(
        title: "Payment failed",
        tone: "danger",
        icon: "!",
        heading: "Something went wrong",
        message: "We couldn't process that payment, so your seats haven't been confirmed yet. Return to the Hiking Spot app to try again.");

    private static ContentResult Page(string title, string tone, string icon, string heading, string message)
    {
        var (background, accent) = tone switch
        {
            "success" => ("#f0fdf4", "#16a34a"),
            "danger" => ("#fef2f2", "#dc2626"),
            _ => ("#f8fafc", "#475569"),
        };

        var html = $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1" />
                <title>{{title}} — Hiking Spot</title>
                <style>
                    body {
                        margin: 0;
                        min-height: 100vh;
                        display: flex;
                        align-items: center;
                        justify-content: center;
                        background: {{background}};
                        font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
                        color: #0f172a;
                    }
                    .card {
                        max-width: 420px;
                        margin: 24px;
                        padding: 40px 32px;
                        text-align: center;
                    }
                    .icon {
                        width: 64px;
                        height: 64px;
                        line-height: 64px;
                        margin: 0 auto 20px;
                        border-radius: 50%;
                        background: {{accent}};
                        color: #ffffff;
                        font-size: 28px;
                    }
                    h1 { font-size: 22px; margin: 0 0 12px; }
                    p { font-size: 15px; line-height: 1.5; color: #475569; margin: 0; }
                </style>
            </head>
            <body>
                <div class="card">
                    <div class="icon">{{icon}}</div>
                    <h1>{{heading}}</h1>
                    <p>{{message}}</p>
                </div>
            </body>
            </html>
            """;

        return new ContentResult { Content = html, ContentType = "text/html; charset=utf-8", StatusCode = 200 };
    }
}
