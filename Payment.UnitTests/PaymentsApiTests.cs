using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Payment.Application.DTOs;

namespace Payment.UnitTests;

public class PaymentsApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetBalance_ExistingAccount_ReturnsCorrectBalance()
    {
        // Act
        var response = await _client.GetAsync("/api/accounts/ACC_DEBIT_001/balance");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("ACC_DEBIT_001", content);
    }

    [Fact]
    public async Task CreatePayment_ValidRequest_ReturnsOkAndUpdatesBalance()
    {
        // Arrange
        var request = new PaymentRequest
        {
            TransactionId = "TEST_TX_" + Guid.NewGuid().ToString().Substring(0, 8),
            SenderAccount = "ACC_DEBIT_001",
            ReceiverAccount = "ACC_CREDIT_001",
            Amount = 100,
            Currency = 860
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/payments", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaymentResult>();
        
        Assert.NotNull(result);
        Assert.Equal(request.TransactionId, result.TransactionId);
        Assert.Equal(0, result.Resp);
        Assert.Equal("COMPLETED", result.Status);
    }

    [Fact]
    public async Task CreatePayment_InvalidData_ReturnsBadRequest()
    {
        // Arrange - Amount is negative, which violates [Range] validation
        var request = new PaymentRequest
        {
            TransactionId = "TEST_TX_ERR",
            SenderAccount = "ACC_DEBIT_001",
            ReceiverAccount = "ACC_CREDIT_001",
            Amount = -50,
            Currency = 860
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}