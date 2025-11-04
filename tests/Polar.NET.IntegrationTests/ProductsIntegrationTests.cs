using FluentAssertions;
using Polar.NET.Models.Products;
using Xunit;

namespace Polar.NET.IntegrationTests;

/// <summary>
/// Integration tests for Products API.
/// </summary>
public class ProductsIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public ProductsIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ProductsApi_CreateProductWithPrices_WorksCorrectly()
    {
        // Arrange
        var client = _fixture.CreateClient();
        var createRequest = new ProductCreateRequest
        {
            Name = $"Test Product {Guid.NewGuid()}",
            Description = "Integration test product with prices",
            Type = ProductType.OneTime
        };

        // Act
        var createdProduct = await client.Products.CreateAsync(createRequest);
        
        // Create multiple prices
        var priceRequest1 = new ProductPriceCreateRequest
        {
            Amount = 1000, // $10.00 in cents
            Currency = "USD",
            Type = ProductPriceType.OneTime
        };
        
        var priceRequest2 = new ProductPriceCreateRequest
        {
            Amount = 2000, // $20.00 in cents
            Currency = "EUR",
            Type = ProductPriceType.OneTime
        };

        var price1 = await client.Products.CreatePriceAsync(createdProduct.Id, priceRequest1);
        var price2 = await client.Products.CreatePriceAsync(createdProduct.Id, priceRequest2);

        // Retrieve product with prices
        var retrievedProduct = await client.Products.GetAsync(createdProduct.Id);

        // Assert
        createdProduct.Should().NotBeNull();
        createdProduct.Id.Should().NotBeNullOrEmpty();
        createdProduct.Name.Should().Be(createRequest.Name);
        createdProduct.Description.Should().Be(createRequest.Description);

        price1.Should().NotBeNull();
        price1.Id.Should().NotBeNullOrEmpty();
        price1.Amount.Should().Be((int)priceRequest1.Amount);
        price1.Currency.Should().Be(priceRequest1.Currency);

        price2.Should().NotBeNull();
        price2.Id.Should().NotBeNullOrEmpty();
        price2.Amount.Should().Be((int)priceRequest2.Amount);
        price2.Currency.Should().Be(priceRequest2.Currency);

        retrievedProduct.Should().NotBeNull();
        retrievedProduct.Prices.Should().HaveCount(2);

        // Cleanup
        await client.Products.ArchiveAsync(createdProduct.Id);
    }

    [Fact]
    public async Task ProductsApi_CreateSubscriptionProduct_WorksCorrectly()
    {
        // Arrange
        var client = _fixture.CreateClient();
        var createRequest = new ProductCreateRequest
        {
            Name = $"Test Subscription {Guid.NewGuid()}",
            Description = "Integration test subscription product",
            Type = ProductType.Subscription,
            IsSubscription = true
        };

        // Act
        var createdProduct = await client.Products.CreateAsync(createRequest);
        
        // Create recurring price
        var priceRequest = new ProductPriceCreateRequest
        {
            Amount = 1999, // $19.99 in cents
            Currency = "USD",
            Type = ProductPriceType.Recurring,
            RecurringInterval = "month"
        };

        var price = await client.Products.CreatePriceAsync(createdProduct.Id, priceRequest);

        // Assert
        createdProduct.Should().NotBeNull();
        createdProduct.Type.Should().Be(ProductType.Subscription);
        createdProduct.IsSubscription.Should().BeTrue();

        price.Should().NotBeNull();
        price.Type.Should().Be(ProductPriceType.Recurring);
        price.RecurringInterval.Should().Be(RecurringInterval.Month);

        // Cleanup
        await client.Products.ArchiveAsync(createdProduct.Id);
    }

    [Fact]
    public async Task ProductsApi_QueryBuilder_WorksCorrectly()
    {
        // Arrange
        var client = _fixture.CreateClient();

        // Act
        var query = client.Products.Query()
            .WithActive(true)
            .WithType("one_time");

        var result = await client.Products.ListAsync(query, limit: 5);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().NotBeNull();
        result.Pagination.Should().NotBeNull();
    }

    [Fact]
    public async Task ProductsApi_Export_WorksCorrectly()
    {
        // Arrange
        var client = _fixture.CreateClient();

        // Act
        var exportResult = await client.Products.ExportAsync();

        // Assert
        exportResult.Should().NotBeNull();
        exportResult.ExportUrl.Should().NotBeNullOrEmpty();
        exportResult.Size.Should().BeGreaterThan(0);
        exportResult.RecordCount.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task ProductsApi_ExportPrices_WorksCorrectly()
    {
        // Arrange
        var client = _fixture.CreateClient();
        
        // First create a product to export prices for
        var productRequest = new ProductCreateRequest
        {
            Name = $"Test Product {Guid.NewGuid()}",
            Type = ProductType.OneTime
        };

        var product = await client.Products.CreateAsync(productRequest);

        // Act
        var exportResult = await client.Products.ExportPricesAsync(product.Id);

        // Assert
        exportResult.Should().NotBeNull();
        exportResult.ExportUrl.Should().NotBeNullOrEmpty();
        exportResult.Size.Should().BeGreaterThan(0);
        exportResult.RecordCount.Should().BeGreaterOrEqualTo(0);

        // Cleanup
        await client.Products.ArchiveAsync(product.Id);
    }
}