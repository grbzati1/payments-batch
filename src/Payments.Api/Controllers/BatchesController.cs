using Microsoft.AspNetCore.Mvc;
using Payments.Api.Contracts;
using Payments.Application.Commands;
using Payments.Application.Services;

namespace Payments.Api.Controllers;

/// <summary>
/// API controller for managing payment batches.
/// Provides endpoints for creating, submitting, and retrieving batch information and associated payments.
/// </summary>
/// <param name="batchService">Service for handling batch operations</param>
[ApiController]
[Route("api/batches")]
public class BatchesController(IBatchService batchService) : ControllerBase
{
    /// <summary>
    /// Creates a new payment batch with the specified payments.
    /// Requires an X-Request-Id header for idempotency support.
    /// </summary>
    /// <param name="request">The batch creation request containing client reference and payment details</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>
    /// Returns 201 Created with the batch ID and status for new batches,
    /// or 200 OK with duplicate flag if the request ID was previously processed
    /// </returns>
    /// <response code="201">Batch successfully created</response>
    /// <response code="200">Duplicate request detected, returning existing batch</response>
    /// <response code="400">X-Request-Id header is missing or invalid</response>
    [HttpPost]
    public async Task<IActionResult> CreateBatch([FromBody] CreateBatchRequest request, CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue("X-Request-Id", out var requestIdValues) || string.IsNullOrWhiteSpace(requestIdValues.ToString()))
        {
            return BadRequest(new { error = "X-Request-Id header is required." });
        }

        var command = new CreateBatchCommand(
            request.ClientBatchReference,
            request.Payments.Select(x => new CreatePaymentItem(
                x.ClientPaymentReference,
                x.Currency,
                x.Amount,
                x.BeneficiaryName,
                x.DestinationAccount)).ToList());

        var (batchId, isDuplicate) = await batchService.CreateBatchAsync(command, requestIdValues.ToString(), cancellationToken);
        var result = await batchService.GetBatchAsync(batchId, cancellationToken);

        return isDuplicate
            ? Ok(new { batchId, status = result?.Status ?? "Draft", duplicate = true })
            : CreatedAtAction(nameof(GetBatch), new { batchId }, new { batchId, status = result?.Status ?? "Draft", duplicate = false });
    }

    /// <summary>
    /// Submits a previously created batch for processing.
    /// </summary>
    /// <param name="batchId">The unique identifier of the batch to submit</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Returns 200 OK with the batch ID and updated status</returns>
    /// <response code="200">Batch successfully submitted</response>
    [HttpPost("{batchId:guid}/submit")]
    public async Task<IActionResult> SubmitBatch(Guid batchId, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;
        await batchService.SubmitBatchAsync(new SubmitBatchCommand(batchId), correlationId, cancellationToken);
        var batch = await batchService.GetBatchAsync(batchId, cancellationToken);
        return Ok(new { batchId, status = batch?.Status ?? "Submitted" });
    }

    /// <summary>
    /// Retrieves the details of a specific batch.
    /// </summary>
    /// <param name="batchId">The unique identifier of the batch to retrieve</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Returns 200 OK with the batch details, or 404 Not Found if the batch doesn't exist</returns>
    /// <response code="200">Batch found and returned</response>
    /// <response code="404">Batch not found</response>
    [HttpGet("{batchId:guid}")]
    public async Task<IActionResult> GetBatch(Guid batchId, CancellationToken cancellationToken)
    {
        var batch = await batchService.GetBatchAsync(batchId, cancellationToken);
        return batch is null ? NotFound() : Ok(batch);
    }

    /// <summary>
    /// Retrieves all payments associated with a specific batch.
    /// </summary>
    /// <param name="batchId">The unique identifier of the batch</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>Returns 200 OK with a collection of payments in the batch</returns>
    /// <response code="200">Payments retrieved successfully</response>
    [HttpGet("{batchId:guid}/payments")]
    public async Task<IActionResult> GetPayments(Guid batchId, CancellationToken cancellationToken)
    {
        var payments = await batchService.GetBatchPaymentsAsync(batchId, cancellationToken);
        return Ok(payments);
    }
}
