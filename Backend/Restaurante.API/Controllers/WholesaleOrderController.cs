using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Application.DTOs;
using Restaurante.Application.Interfaces;
using Restaurante.Domain.Entities;

namespace Restaurante.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = UserRoles.Admin + "," + UserRoles.Manager)]
public class WholesaleOrderController : ControllerBase
{
    private readonly IWholesaleOrderService _service;

    public WholesaleOrderController(IWholesaleOrderService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Create(WholesaleOrderRequest request)
    {
        var created = await _service.CreateAsync(request);
        return created is null ? BadRequest() : Created($"/api/wholesaleorder/{created.Id}", created);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var order = await _service.GetByIdAsync(id);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpGet("branch/{branchId:int}")]
    public async Task<IActionResult> GetByBranch(int branchId, [FromQuery] string? status) =>
        Ok(await _service.GetByBranchAsync(branchId, status));

    [HttpPost("{id:long}/approve")]
    public async Task<IActionResult> Approve(long id)
    {
        var result = await _service.ApproveAsync(id);
        return result is null ? BadRequest(new { message = "No se puede aprobar este pedido." }) : Ok(result);
    }

    [HttpPost("{id:long}/receive")]
    public async Task<IActionResult> Receive(long id)
    {
        var result = await _service.MarkAsReceivedAsync(id);
        return result is null ? BadRequest(new { message = "El pedido debe estar aprobado para recibirlo." }) : Ok(result);
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id)
    {
        var result = await _service.CancelAsync(id);
        return result is null ? BadRequest() : Ok(result);
    }
}
