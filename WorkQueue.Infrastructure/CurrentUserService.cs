using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;

namespace WorkQueue.Infrastructure
{
    public interface ICurrentUserService
    {
        Guid? GetOrganizationId();
        Guid? GetUserId();
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? GetOrganizationId()
        {
            var orgIdStr = _httpContextAccessor.HttpContext?.User?.FindFirst("OrganizationId")?.Value;
            return Guid.TryParse(orgIdStr, out var orgId) ? orgId : null;
        }

        public Guid? GetUserId()
        {
            // ClaimTypes.NameIdentifier 
            var userIdStr = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? _httpContextAccessor.HttpContext?.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(userIdStr, out var userId) ? userId : null;
        }
    }
}