namespace Simcag.IdentityService.Application.UseCases.GetProfile;

using MediatR;
using Simcag.IdentityService.Application.DTOs;

public sealed record GetProfileQuery(Guid UserId, Guid TenantId) : IRequest<UserProfileDto?>;
