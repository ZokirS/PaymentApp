using Microsoft.AspNetCore.Mvc;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Infrastructure.Repositories;

namespace PaymentProcessing.API.Controllers;

[ApiController]
[Route("api")]
public class PaymentsController : ControllerBase
{

    private readonly IPaymentService _paymentService;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentsController(IPaymentService paymentService, IUnitOfWork unitOfWork)
    {
        _paymentService = paymentService;
        _unitOfWork = unitOfWork;
    }

    [HttpPost("payments")]
    public async Task<IActionResult> CreatePayment([FromBody] PaymentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _paymentService.ProcessPaymentAsync(request);
            return Ok(result);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpGet("payments/{transactionId}")]
    public async Task<IActionResult> GetPaymentStatus(string transactionId)
    {
        var tx = await _unitOfWork.Transactions.GetByIdAsync(transactionId);

        if (tx == null)
            return NotFound(new { Error = "Транзакция не найдена" });

        return Ok(new
        {
            TransactionId = tx.TransactionId,
            Status = tx.Status.ToString(),
            Amount = tx.Amount,
            Currency = tx.Currency,
            CreatedAt = tx.CreatedAt
        });
    }

    [HttpGet("accounts/{accountId}/balance")]
    public async Task<IActionResult> GetBalance(string accountId)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);

        if (account == null)
            return NotFound(new { Error = "Счет не найден" });

        return Ok(new
        {
            AccountId = account.Id,
            Balance = account.Balance,
            Currency = account.Currency
        });
    }
}