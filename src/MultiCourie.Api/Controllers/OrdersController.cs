using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MultiCourie.Application.Orders;
using MultiCourie.Application.Orders.Dtos;

namespace MultiCourie.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IValidator<CreateOrderRequestDto> _createValidator;
    private readonly IValidator<BulkCreateOrdersRequestDto> _bulkValidator;

    public OrdersController(
        IOrderService orderService,
        IValidator<CreateOrderRequestDto> createValidator,
        IValidator<BulkCreateOrdersRequestDto> bulkValidator)
    {
        _orderService = orderService;
        _createValidator = createValidator;
        _bulkValidator = bulkValidator;
    }
    /// <summary>
    /// CreateOrder
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var result = await _orderService.CreateOrderAsync(request, cancellationToken);
        return result.IdempotentReplay
            ? Ok(result)
            : CreatedAtAction(nameof(TrackOrder), new { orderId = result.OrderId }, result);
    }
    /// <summary>
    /// Track Order
    /// </summary>
    /// <param name="orderId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("{orderId}/track")]

    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(TrackOrderResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> TrackOrder([FromRoute] string orderId, CancellationToken cancellationToken)
    {
        var result = await _orderService.TrackOrderAsync(orderId, cancellationToken);
        return Ok(result);
    }
    /// <summary>
    /// Cancel Order 
    /// </summary>
    /// <param name="orderId"></param>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("{orderId}/cancel")]

    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelOrder(
        [FromRoute] string orderId,
        [FromBody] CancelOrderRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CourierPartner))
        {
            return BadRequest(new { message = "courier_partner is required." });
        }

        var result = await _orderService.CancelOrderAsync(orderId, request, cancellationToken);
        return Ok(result);
    }
    /// <summary>
    /// Bulk Create Orders
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("bulk")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(BulkCreateAcceptedResponseDto), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> BulkCreate([FromBody] BulkCreateOrdersRequestDto request, CancellationToken cancellationToken)
    {
        await _bulkValidator.ValidateAndThrowAsync(request, cancellationToken);
        var accepted = await _orderService.EnqueueBulkCreateAsync(request, cancellationToken);
        return AcceptedAtAction(nameof(GetBulkStatus), new { batchId = accepted.BatchId }, accepted);
    }
    /// <summary>
    /// get Bulk Status
    /// </summary>
    /// <param name="batchId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("bulk/{batchId:guid}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(BulkBatchStatusResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBulkStatus([FromRoute] Guid batchId, CancellationToken cancellationToken)
    {
        var result = await _orderService.GetBulkBatchStatusAsync(batchId, cancellationToken);
        return Ok(result);
    }
}
