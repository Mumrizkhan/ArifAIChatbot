namespace TenantManagementService.Models;

public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? CustomDomain { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PrimaryColor { get; set; } = string.Empty;
    public string SecondaryColor { get; set; } = string.Empty;
    public string DefaultLanguage { get; set; } = string.Empty;
    public bool IsRtlEnabled { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? LogoUrl { get; set; }
    public string? Role { get; set; }
}

public class CreateTenantRequest
{
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? CustomDomain { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? DefaultLanguage { get; set; }
    public bool IsRtlEnabled { get; set; } = false;
}

public class UpdateTenantRequest
{
    public string? Name { get; set; }
    public string? CustomDomain { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? DefaultLanguage { get; set; }
    public bool IsRtlEnabled { get; set; } = false;
    public string? LogoUrl { get; set; }
}

public class TenantSettingsDto
{
    public Guid TenantId { get; set; }
    public Dictionary<string, object> Settings { get; set; } = new();
}

public class UpdateTenantSettingsRequest
{
    public Dictionary<string, object> Settings { get; set; } = new();
}

public class TenantStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalConversations { get; set; }
    public int TotalMessages { get; set; }
    public int ActiveChatbots { get; set; }
}

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class ChatbotConfigDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class CreateChatbotConfigRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = new();
}

public class UpdateChatbotConfigRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public Dictionary<string, object>? Configuration { get; set; }
}

public class TeamMemberDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class AddTeamMemberRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UpdateTeamMemberRequest
{
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}

public class BulkUpdateMembersRequest
{
    public List<Guid> MemberIds { get; set; } = new();
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}
