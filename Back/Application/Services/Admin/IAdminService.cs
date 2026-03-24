using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using System.Security.Claims;

namespace Back.Api.Application.Services;

public interface IAdminService
{
    Task<ApplicationResult> GetAllAsync();
    Task<ApplicationResult> CreateAsync(CreateAdminDto dto, ClaimsPrincipal user);
}
