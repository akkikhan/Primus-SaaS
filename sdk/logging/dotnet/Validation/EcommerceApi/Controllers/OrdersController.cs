using Microsoft.AspNetCore.Mvc;
using PrimusSaaS.Logging.Core;
using EcommerceApi.Models;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly Logger _logger;
    private static readonly List<Order> _orders = new();

    public OrdersController(Logger logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public IActionResult CreateOrder([FromBody] CreateOrderRequest request)
    {
        var correlationId = _logger.GenerateCorrelationId();
        var timer = _logger.StartTimer();

        _logger.Info("Order creation started", new Dictionary<string, object>
        {
            ["correlationId"] = correlationId,
            ["itemCount"] = request.Items.Count
        });

        try
        {
            // Validate items
            if (!request.Items.Any())
            {
                _logger.Warn("Order creation failed - no items", new Dictionary<string, object>
                {
                    ["correlationId"] = correlationId
                });
                return BadRequest(new { error = "Order must contain at least one item" });
            }

            // Calculate total
            var total = request.Items.Sum(item => item.Price * item.Quantity);

            _logger.Info("Order total calculated", new Dictionary<string, object>
            {
                ["correlationId"] = correlationId,
                ["total"] = total
            });

            // Create order
            var order = new Order
            {
                Id = $"ord-{Guid.NewGuid():N}",
                UserId = HttpContext.Items["PrimusUser"] is Dictionary<string, object> user 
                    ? user["userId"]?.ToString() ?? "anonymous"
                    : "anonymous",
                Items = request.Items,
                Total = total,
                Status = "pending"
            };

            _orders.Add(order);

            _logger.Info("Order created successfully", new Dictionary<string, object>
            {
                ["correlationId"] = correlationId,
                ["orderId"] = order.Id,
                ["userId"] = order.UserId,
                ["total"] = total,
                ["itemCount"] = order.Items.Count
            });

            // Simulate inventory reservation
            _logger.Info("Reserving inventory", new Dictionary<string, object>
            {
                ["correlationId"] = correlationId,
                ["orderId"] = order.Id
            });

            Thread.Sleep(50); // Simulate work

            _logger.Info("Inventory reserved", new Dictionary<string, object>
            {
                ["correlationId"] = correlationId,
                ["orderId"] = order.Id
            });

            // Simulate payment processing
            _logger.Info("Processing payment", new Dictionary<string, object>
            {
                ["correlationId"] = correlationId,
                ["orderId"] = order.Id,
                ["amount"] = total
            });

            Thread.Sleep(100); // Simulate work

            order.Status = "confirmed";

            _logger.Info("Payment processed successfully", new Dictionary<string, object>
            {
                ["correlationId"] = correlationId,
                ["orderId"] = order.Id,
                ["status"] = order.Status
            });

            timer.Done("Order creation completed", new Dictionary<string, object>
            {
                ["correlationId"] = correlationId,
                ["orderId"] = order.Id
            });

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            _logger.Error("Order creation failed", new Dictionary<string, object>
            {
                ["correlationId"] = correlationId,
                ["error"] = ex.Message,
                ["stackTrace"] = ex.StackTrace ?? string.Empty
            });
            throw;
        }
    }

    [HttpGet("{id}")]
    public IActionResult GetOrder(string id)
    {
        _logger.Info("Fetching order", new Dictionary<string, object>
        {
            ["orderId"] = id
        });

        var order = _orders.FirstOrDefault(o => o.Id == id);

        if (order == null)
        {
            _logger.Warn("Order not found", new Dictionary<string, object>
            {
                ["orderId"] = id
            });
            return NotFound();
        }

        return Ok(order);
    }

    [HttpGet]
    public IActionResult GetUserOrders()
    {
        var userId = HttpContext.Items["PrimusUser"] is Dictionary<string, object> user
            ? user["userId"]?.ToString() ?? "anonymous"
            : "anonymous";

        _logger.Info("Fetching user orders", new Dictionary<string, object>
        {
            ["userId"] = userId
        });

        var userOrders = _orders.Where(o => o.UserId == userId).ToList();

        return Ok(userOrders);
    }

    [HttpPost("{id}/cancel")]
    public IActionResult CancelOrder(string id)
    {
        var order = _orders.FirstOrDefault(o => o.Id == id);

        if (order == null)
        {
            _logger.Warn("Cannot cancel - order not found", new Dictionary<string, object>
            {
                ["orderId"] = id
            });
            return NotFound();
        }

        if (order.Status == "cancelled")
        {
            _logger.Warn("Order already cancelled", new Dictionary<string, object>
            {
                ["orderId"] = id
            });
            return BadRequest(new { error = "Order already cancelled" });
        }

        var oldStatus = order.Status;
        order.Status = "cancelled";

        _logger.Info("Order cancelled", new Dictionary<string, object>
        {
            ["orderId"] = id,
            ["oldStatus"] = oldStatus,
            ["newStatus"] = "cancelled"
        });

        return Ok(order);
    }
}
