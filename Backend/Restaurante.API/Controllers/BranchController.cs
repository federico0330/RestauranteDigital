using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Application.DTOs;
using Restaurante.Application.Interfaces;
using Restaurante.Domain.Entities;

namespace Restaurante.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class BranchController : ControllerBase
{
    private readonly IBranchService _branches;

    public BranchController(IBranchService branches) => _branches = branches;

    [HttpGet]
    public async Task<IActionResult> GetActive() => Ok(await _branches.GetActiveBranchesAsync());

    [HttpPost]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> Create(BranchRequest request)
    {
        var created = await _branches.CreateBranchAsync(request);
        return created is null ? BadRequest() : Created($"/api/branch/{created.Id}", created);
    }

    [HttpGet("{branchId:int}/stock")]
    [Authorize]
    public async Task<IActionResult> GetStock(int branchId) =>
        Ok(await _branches.GetStockByBranchAsync(branchId));

    [HttpPut("{branchId:int}/stock/{dishId:guid}")]
    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Manager)]
    public async Task<IActionResult> AdjustStock(int branchId, Guid dishId, StockAdjustmentRequest request)
    {
        var result = await _branches.AdjustStockAsync(branchId, dishId, request);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("stock/alerts")]
    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Manager)]
    public async Task<IActionResult> GetLowStockAlerts() =>
        Ok(await _branches.GetLowStockAlertsAsync());
}
