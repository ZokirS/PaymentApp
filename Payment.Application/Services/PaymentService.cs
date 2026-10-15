using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Repositories;

namespace Payment.Application.Services;
public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExternalBankClient _bankClient;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IUnitOfWork unitOfWork, IExternalBankClient bankClient, ILogger<PaymentService> logger)
    {
        _unitOfWork = unitOfWork;
        _bankClient = bankClient;
        _logger = logger;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        // 1. Verifying idempotency via a transaction repository
        var existingTx = await _unitOfWork.Transactions.GetByIdAsync(request.TransactionId);
        if (existingTx != null)
        {
            return new PaymentResult
            {
                TransactionId = existingTx.TransactionId,
                Resp = existingTx.Status == TransactionStatusEnum.Completed ? 0 : 1,
                Status = existingTx.Status.ToString().ToUpper()
            };
        }

        try
        {
            var sender = await _unitOfWork.Accounts.GetByIdAsync(request.SenderAccount);
            var receiver = await _unitOfWork.Accounts.GetByIdAsync(request.ReceiverAccount);

            if (sender == null || receiver == null)
                throw new ArgumentException("Счет отправителя или получателя не найден.");

            if (sender.Balance < request.Amount)
                throw new InvalidOperationException("Недостаточно средств на счете.");

            // Creation transaction with Pending status
            var paymentTx = new PaymentTransaction
            {
                TransactionId = request.TransactionId,
                SenderAccountId = request.SenderAccount,
                ReceiverAccountId = request.ReceiverAccount,
                Amount = request.Amount,
                Currency = request.Currency,
                Status = TransactionStatusEnum.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Transactions.AddAsync(paymentTx);
            await _unitOfWork.SaveChangesAsync(); // Saving Pending

            // 2. Calling external service
            bool bankSuccess;
            try
            {
                bankSuccess = await _bankClient.ExecuteTransferAsync(request);
            }
            catch (TimeoutException)
            {
                return new PaymentResult { TransactionId = request.TransactionId, Resp = 1, Status = "PENDING" };
            }

            // 3. Recording the result
            if (bankSuccess)
            {
                sender.Balance -= request.Amount;
                sender.Version++; 
    
                receiver.Balance += request.Amount;
                receiver.Version++;
    
                _unitOfWork.Accounts.Update(sender);
                _unitOfWork.Accounts.Update(receiver);
    
                paymentTx.Status = TransactionStatusEnum.Completed;
            }
            else
            {
                paymentTx.Status = TransactionStatusEnum.Rejected;
            }

            _unitOfWork.Transactions.Update(paymentTx);
            await _unitOfWork.SaveChangesAsync();

            return new PaymentResult 
            { 
                TransactionId = request.TransactionId, 
                Resp = bankSuccess ? 0 : -1, 
                Status = paymentTx.Status.ToString().ToUpper() 
            };
        }
        catch (DbUpdateException)
        {
            return new PaymentResult { TransactionId = request.TransactionId, Resp = 1, Status = "ALREADY_PROCESSING" };
        }
    }
}