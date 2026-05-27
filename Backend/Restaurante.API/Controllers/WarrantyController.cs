using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurante.Application.DTOs;
using Restaurante.Application.Interfaces;
using Restaurante.Domain.Entities;

namespace Restaurante.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class WarrantyController : ControllerBase
{
    private readonly IWarrantyService _service;

    public WarrantyController(IWarrantyService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Create(WarrantyClaimRequest request)
    {
        var created = await _service.CreateAsync(request);
        return created is null ? BadRequest(new { message = "No se encontró la orden o el ítem." }) : Created($"/api/warranty/{created.Id}", created);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var claim = await _service.GetByIdAsync(id);
        return claim is null ? NotFound() : Ok(claim);
    }

    [HttpGet]
    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Manager)]
    public async Task<IActionResult> GetByStatus([FromQuery] string status = "Pending") =>
        Ok(await _service.GetByStatusAsync(status));

    [HttpPost("{id:long}/resolve")]
    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Manager)]
    public async Task<IActionResult> Resolve(long id, WarrantyClaimResolveRequest request)
    {
        var result = await _service.ResolveAsync(id, request);
        return result is null ? BadRequest(new { message = "No se puede resolver este reclamo." }) : Ok(result);
    }
}
