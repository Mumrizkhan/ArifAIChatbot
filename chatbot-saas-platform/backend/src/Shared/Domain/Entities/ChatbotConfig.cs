using Shared.Domain.Common;
using System;
using System.Collections.Generic;

namespace Shared.Domain.Entities;

public class ChatbotConfig : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Language { get; set; } = "en";
    public bool IsActive { get; set; } = true;
   
    public Dictionary<string, object>? Configuration { get; set; }
}