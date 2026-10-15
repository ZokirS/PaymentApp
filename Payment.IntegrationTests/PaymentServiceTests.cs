using Microsoft.Extensions.Logging;
using Moq;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Application.Services;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Infrastructure.Repositories;

namespace Payment.IntegrationTests;

public class PaymentServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IExternalBankClient> _bankClientMock;
    private readonly PaymentService _paymentService;

    public PaymentServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _bankClientMock = new Mock<IExternalBankClient>();
        var loggerMock = new Mock<ILogger<PaymentService>>();
        
        _paymentService = new PaymentService(
            _unitOfWorkMock.Object, 
            _bankClientMock.Object, 
            loggerMock.Object);
    }

    [Fact]
    public async Task ProcessPayment_Idempotency_ReturnsExistingTransaction()
    {
        // Arrange
        var request = new PaymentRequest { TransactionId = "TX_123" };
        _unitOfWorkMock.Setup(u => u.Transactions.GetByIdAsync("TX_123"))
            .ReturnsAsync(new PaymentTransaction { TransactionId = "TX_123", Status = TransactionStatusEnum.Completed });

        // Act
        var result = await _paymentService.ProcessPaymentAsync(request);

        // Assert
        Assert.Equal(0, result.Resp);
        Assert.Equal("COMPLETED", result.Status);
        _unitOfWorkMock.Verify(u => u.Accounts.GetByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPayment_InsufficientFunds_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new PaymentRequest 
        { 
            TransactionId = "TX_124", SenderAccount = "ACC_1", ReceiverAccount = "ACC_2", Amount = 1000 
        };
        
        _unitOfWorkMock.Setup(u => u.Transactions.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((PaymentTransaction?)null);
            
        _unitOfWorkMock.Setup(u => u.Accounts.GetByIdAsync("ACC_1"))
            .ReturnsAsync(new Account { Id = "ACC_1", Balance = 500 }); // Balance < Amount
            
        _unitOfWorkMock.Setup(u => u.Accounts.GetByIdAsync("ACC_2"))
            .ReturnsAsync(new Account { Id = "ACC_2", Balance = 0 });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _paymentService.ProcessPaymentAsync(request));
    }

    [Fact]
    public async Task ProcessPayment_SuccessfulTransfer_UpdatesBalances()
    {
        // Arrange
        var request = new PaymentRequest 
        { 
            TransactionId = "TX_125", SenderAccount = "ACC_1", ReceiverAccount = "ACC_2", Amount = 500, Currency = 860 
        };
        
        var sender = new Account { Id = "ACC_1", Balance = 1000, Currency = 860 };
        var receiver = new Account { Id = "ACC_2", Balance = 0, Currency = 860 };

        _unitOfWorkMock.Setup(u => u.Transactions.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((PaymentTransaction?)null);
        _unitOfWorkMock.Setup(u => u.Accounts.GetByIdAsync("ACC_1")).ReturnsAsync(sender);
        _unitOfWorkMock.Setup(u => u.Accounts.GetByIdAsync("ACC_2")).ReturnsAsync(receiver);
        _bankClientMock.Setup(b => b.ExecuteTransferAsync(request)).ReturnsAsync(true);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(request);

        // Assert
        Assert.Equal(0, result.Resp);
        Assert.Equal("COMPLETED", result.Status);
        Assert.Equal(500, sender.Balance);
        Assert.Equal(500, receiver.Balance);
        
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2)); // Once for Pending, once for Completed
    }
}