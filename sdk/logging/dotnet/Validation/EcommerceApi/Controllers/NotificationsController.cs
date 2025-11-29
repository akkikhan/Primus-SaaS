using Microsoft.AspNetCore.Mvc;
using PrimusSaaS.Logging.Core;
using PrimusSaaS.Notifications.Core;
using System.Linq;

namespace EcommerceApi.Controllers;

/// <summary>
/// Controller to test PrimusSaaS.Notifications package functionality
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly Logger _logger;
    private readonly NotificationService _notificationService;

    public NotificationsController(Logger logger, NotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Send a test email notification
    /// </summary>
    [HttpPost("email")]
    public async Task<IActionResult> SendTestEmail([FromBody] SendEmailRequest request)
    {
        var correlationId = _logger.GenerateCorrelationId();

        _logger.Info("Sending test email notification", new Dictionary<string, object?>
        {
            ["correlationId"] = correlationId,
            ["to"] = request.To,
            ["subject"] = request.Subject
        });

        try
        {
            var result = await _notificationService.SendEmailAsync(
                request.To,
                request.Subject,
                request.Body,
                request.Name
            );

            if (!result.Success)
            {
                _logger.Error("Email notification failed", new Dictionary<string, object?>
                {
                    ["correlationId"] = correlationId,
                    ["failureReason"] = result.FailureReason,
                    ["channels"] = result.Channels
                });

                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    error = result.FailureReason ?? "Email delivery failed",
                    correlationId,
                    channels = result.Channels.Select(c => new { c.Channel, Status = c.Status.ToString(), c.Detail })
                });
            }

            _logger.Info("Email sent successfully", new Dictionary<string, object?>
            {
                ["correlationId"] = correlationId,
                ["channel"] = result.ChannelUsed
            });

            return Ok(new { message = "Email sent successfully", correlationId, channel = result.ChannelUsed });
        }
        catch (NotificationFailedException ex)
        {
            _logger.Error(ex, "Failed to send email", new Dictionary<string, object?>
            {
                ["correlationId"] = correlationId,
                ["error"] = ex.Message,
                ["failureReason"] = ex.Result.FailureReason
            });

            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = ex.Result.FailureReason ?? ex.Message,
                correlationId,
                channels = ex.Result.Channels.Select(c => new { c.Channel, Status = c.Status.ToString(), c.Detail })
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to send email", new Dictionary<string, object?>
            {
                ["correlationId"] = correlationId,
                ["error"] = ex.Message
            });

            return StatusCode(500, new { error = ex.Message, correlationId });
        }
    }

    /// <summary>
    /// Send a test SMS notification (requires Twilio configuration)
    /// </summary>
    [HttpPost("sms")]
    public async Task<IActionResult> SendTestSms([FromBody] SendSmsRequest request)
    {
        var correlationId = _logger.GenerateCorrelationId();

        _logger.Info("Sending test SMS notification", new Dictionary<string, object?>
        {
            ["correlationId"] = correlationId,
            ["to"] = request.To
        });

        try
        {
            var result = await _notificationService.SendSmsAsync(request.To, request.Message);

            if (!result.Success)
            {
                _logger.Error("SMS notification failed", new Dictionary<string, object?>
                {
                    ["correlationId"] = correlationId,
                    ["failureReason"] = result.FailureReason,
                    ["channels"] = result.Channels
                });

                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    error = result.FailureReason ?? "SMS delivery failed",
                    correlationId,
                    channels = result.Channels.Select(c => new { c.Channel, Status = c.Status.ToString(), c.Detail })
                });
            }

            _logger.Info("SMS sent successfully", new Dictionary<string, object?>
            {
                ["correlationId"] = correlationId,
                ["channel"] = result.ChannelUsed
            });

            return Ok(new { message = "SMS sent successfully", correlationId, channel = result.ChannelUsed });
        }
        catch (NotificationFailedException ex)
        {
            _logger.Error(ex, "Failed to send SMS", new Dictionary<string, object?>
            {
                ["correlationId"] = correlationId,
                ["error"] = ex.Message,
                ["failureReason"] = ex.Result.FailureReason
            });

            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = ex.Result.FailureReason ?? ex.Message,
                correlationId,
                channels = ex.Result.Channels.Select(c => new { c.Channel, Status = c.Status.ToString(), c.Detail })
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to send SMS", new Dictionary<string, object?>
            {
                ["correlationId"] = correlationId,
                ["error"] = ex.Message
            });

            return StatusCode(500, new { error = ex.Message, correlationId });
        }
    }

    /// <summary>
    /// Send order confirmation notification (demonstrates real-world use case)
    /// </summary>
    [HttpPost("order-confirmation")]
    public async Task<IActionResult> SendOrderConfirmation([FromBody] OrderConfirmationRequest request)
    {
        var correlationId = _logger.GenerateCorrelationId();
        var timer = _logger.StartTimer();

        _logger.Info("Sending order confirmation", new Dictionary<string, object?>
        {
            ["correlationId"] = correlationId,
            ["orderId"] = request.OrderId,
            ["customerEmail"] = request.CustomerEmail
        });

        try
        {
            var emailBody = $@"
<!DOCTYPE html>
<html>
<head><title>Order Confirmation</title></head>
<body>
    <h1>Thank you for your order!</h1>
    <p>Dear {request.CustomerName},</p>
    <p>Your order <strong>#{request.OrderId}</strong> has been confirmed.</p>
    <p>Total: <strong>${request.Total:F2}</strong></p>
    <p>We'll notify you when your order ships.</p>
    <br/>
    <p>Best regards,<br/>E-Commerce Team</p>
</body>
</html>";

            var result = await _notificationService.SendEmailAsync(
                request.CustomerEmail,
                $"Order Confirmation - #{request.OrderId}",
                emailBody,
                request.CustomerName
            );

            if (!result.Success)
            {
                _logger.Error("Order confirmation email failed", new Dictionary<string, object?>
                {
                    ["correlationId"] = correlationId,
                    ["orderId"] = request.OrderId,
                    ["failureReason"] = result.FailureReason,
                    ["channels"] = result.Channels
                });

                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    error = result.FailureReason ?? "Order confirmation email failed",
                    correlationId,
                    channels = result.Channels.Select(c => new { c.Channel, Status = c.Status.ToString(), c.Detail })
                });
            }

            timer.Done("Order confirmation sent", new Dictionary<string, object?>
            {
                ["correlationId"] = correlationId,
                ["orderId"] = request.OrderId,
                ["channel"] = result.ChannelUsed
            });

            return Ok(new 
            { 
                message = "Order confirmation sent", 
                orderId = request.OrderId,
                correlationId,
                channel = result.ChannelUsed
            });
        }
        catch (NotificationFailedException ex)
        {
            _logger.Error(ex, "Failed to send order confirmation", new Dictionary<string, object?>
            {
                ["correlationId"] = correlationId,
                ["orderId"] = request.OrderId,
                ["failureReason"] = ex.Result.FailureReason
            });

            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = ex.Result.FailureReason ?? ex.Message,
                correlationId,
                channels = ex.Result.Channels.Select(c => new { c.Channel, Status = c.Status.ToString(), c.Detail })
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to send order confirmation", new Dictionary<string, object?>
            {
                ["correlationId"] = correlationId,
                ["orderId"] = request.OrderId
            });

            return StatusCode(500, new { error = ex.Message, correlationId });
        }
    }
}

public class SendEmailRequest
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Name { get; set; }
}

public class SendSmsRequest
{
    public string To { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class OrderConfirmationRequest
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
}
