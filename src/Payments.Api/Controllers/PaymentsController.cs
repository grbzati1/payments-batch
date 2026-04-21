using Microsoft.AspNetCore.Mvc;
using Payments.Application.Services;

namespace Payments.Api.Controllers;

/// <summary>
/// Handles HTTP requests for payment operations.
/// </summary>
/// <param name="batchService">The service for managing payment batch operations.</param>
[ApiController]
[Route("api/payments")]
public class PaymentsController(IBatchService batchService) : ControllerBase
{
    [HttpGet("{paymentId:guid}")]
    public async Task<IActionResult> GetPayment(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await batchService.GetPaymentAsync(paymentId, cancellationToken);
        return payment is null ? NotFound() : Ok(payment);
    }
}
