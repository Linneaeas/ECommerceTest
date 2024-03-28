namespace ECommerceTest;
using Xunit;
using System.Net.Http.Json;

public class ExampleIntegrationTests : IClassFixture<ApplicationFactory<ECommerceBE.Program>>
{
    ApplicationFactory<ECommerceBE.Program> factory;

    public ExampleIntegrationTests(ApplicationFactory<ECommerceBE.Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async void AddProduct_WhenCreate_ThenSuccess()
    {
        var client = factory.CreateClient();
        var productName = "Skirt";
        var productDescription = "enbeskrivning";
        var productInventory = 3;
        var productPrice = 150;
        var productPicture = "enbild";

        var request = await client.PostAsync($"/Product/AddProduct?name={productName}&description={productDescription}&inventory={productInventory}&price={productPrice}&picture={productPicture}", null);
        ECommerceBE.Models.ProductDto? response =
            await request.Content.ReadFromJsonAsync<ECommerceBE.Models.ProductDto>();

        request.EnsureSuccessStatusCode();
        Assert.NotNull(response);
        Assert.Equal(productName, response.Name);
        Assert.Equal(productDescription, response.Description);
        Assert.Equal(productInventory, response.Inventory);
        Assert.Equal(productPrice, response.Price);
        Assert.Equal(productPicture, response.Picture);
    }


    [Fact]
    public async void DeleteProduct_WhenExisting_ThenSuccess()
    {
        var client = factory.CreateClient();
        var productName = "Skirt";
        var productDescription = "enbeskrivning";
        var productInventory = 3;
        var productPrice = 150;
        var productPicture = "enbild";

        var addProductRequest = await client.PostAsync($"/Product/AddProduct?name={productName}&description={productDescription}&inventory={productInventory}&price={productPrice}&picture={productPicture}", null);
        var addedProduct = await addProductRequest.Content.ReadFromJsonAsync<ECommerceBE.Models.ProductDto>();

        var deleteRequest = await client.DeleteAsync($"/Product/DeleteProduct/{addedProduct.Id}");

        deleteRequest.EnsureSuccessStatusCode();
        var deleteResponse = await deleteRequest.Content.ReadAsStringAsync();
        Assert.Equal("Product deleted successfully", deleteResponse);
    }
}
