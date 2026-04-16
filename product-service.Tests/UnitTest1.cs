using Moq;
using product_service.Models;
using product_service.Repositories;
using product_service.Services;

namespace product_service.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task ReduceStockAsync_ShouldReturnTrue_AndUpdateStock_WhenStockIsSufficient()
    {
        // Arrange
        var repositoryMock = new Mock<IProductRepository>();
        var product = new Product { Id = 1, Stock = 50, Name = "Notebook" };

        repositoryMock
            .Setup(r => r.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var service = new ProductService(repositoryMock.Object);

        // Act
        var result = await service.ReduceStockAsync(product.Id, 20);

        // Assert
        Assert.True(result);
        Assert.Equal(30, product.Stock);
        repositoryMock.Verify(r => r.UpdateAsync(product), Times.Once);
    }

    [Fact]
    public async Task ReduceStockAsync_ShouldReturnFalse_AndNotUpdate_WhenStockIsInsufficient()
    {
        // Arrange
        var repositoryMock = new Mock<IProductRepository>();
        var product = new Product { Id = 1, Stock = 30, Name = "Notebook" };

        repositoryMock
            .Setup(r => r.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var service = new ProductService(repositoryMock.Object);

        // Act
        var result = await service.ReduceStockAsync(product.Id, 35);

        // Assert
        Assert.False(result);
        Assert.Equal(30, product.Stock);
        repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task ReduceStockAsync_ShouldThrowArgumentException_WhenQuantityIsInvalid()
    {
        // Arrange
        var repositoryMock = new Mock<IProductRepository>();
        var service = new ProductService(repositoryMock.Object);

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.ReduceStockAsync(1, 0));
        repositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task IncreaseStockAsync_ShouldAddStock_AndPersistUpdate()
    {
        // Arrange
        var repositoryMock = new Mock<IProductRepository>();
        var product = new Product { Id = 1, Stock = 10, Name = "Mouse" };

        repositoryMock
            .Setup(r => r.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var service = new ProductService(repositoryMock.Object);

        // Act
        var result = await service.IncreaseStockAsync(product.Id, 5);

        // Assert
        Assert.True(result);
        Assert.Equal(15, product.Stock);
        repositoryMock.Verify(r => r.UpdateAsync(product), Times.Once);
    }
}
