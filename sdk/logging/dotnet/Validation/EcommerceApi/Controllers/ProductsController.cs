using Microsoft.AspNetCore.Mvc;
using PrimusSaaS.Logging.Core;
using EcommerceApi.Models;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly Logger _logger;
    private static readonly List<Product> _products = new()
    {
        new() { Id = "prod-1", Name = "Laptop", Price = 999.99m, Stock = 50, Category = "Electronics" },
        new() { Id = "prod-2", Name = "Mouse", Price = 29.99m, Stock = 200, Category = "Electronics" },
        new() { Id = "prod-3", Name = "Keyboard", Price = 79.99m, Stock = 150, Category = "Electronics" },
        new() { Id = "prod-4", Name = "Monitor", Price = 299.99m, Stock = 75, Category = "Electronics" }
    };

    public ProductsController(Logger logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetAll([FromQuery] string? category = null)
    {
        var timer = _logger.StartTimer();

        _logger.Info("Fetching products", new Dictionary<string, object>
        {
            ["category"] = category ?? "all",
            ["totalProducts"] = _products.Count
        });

        var products = string.IsNullOrEmpty(category)
            ? _products
            : _products.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();

        timer.Done("Products fetched", new Dictionary<string, object>
        {
            ["resultCount"] = products.Count
        });

        return Ok(products);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(string id)
    {
        _logger.Info("Fetching product by ID", new Dictionary<string, object>
        {
            ["productId"] = id
        });

        var product = _products.FirstOrDefault(p => p.Id == id);

        if (product == null)
        {
            _logger.Warn("Product not found", new Dictionary<string, object>
            {
                ["productId"] = id
            });
            return NotFound(new { error = "Product not found" });
        }

        return Ok(product);
    }

    [HttpPost]
    public IActionResult Create([FromBody] Product product)
    {
        product.Id = $"prod-{Guid.NewGuid():N}";

        _logger.Info("Creating new product", new Dictionary<string, object>
        {
            ["productId"] = product.Id,
            ["productName"] = product.Name,
            ["price"] = product.Price,
            ["stock"] = product.Stock
        });

        _products.Add(product);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id}/stock")]
    public IActionResult UpdateStock(string id, [FromBody] int newStock)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);

        if (product == null)
        {
            _logger.Warn("Cannot update stock - product not found", new Dictionary<string, object>
            {
                ["productId"] = id
            });
            return NotFound();
        }

        var oldStock = product.Stock;
        product.Stock = newStock;

        _logger.Info("Stock updated", new Dictionary<string, object>
        {
            ["productId"] = id,
            ["oldStock"] = oldStock,
            ["newStock"] = newStock,
            ["difference"] = newStock - oldStock
        });

        return Ok(product);
    }
}
