using Shared.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Domain.Entities
{
    /// <summary>
    /// Entity for storing analytics events
    /// </summary>
    [Table("AnalyticsEvents")]
    public class AnalyticsEvent:AuditableEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string EventType { get; set; } = null!;

       
        public Dictionary<string, object>? EventData { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        [MaxLength(100)]
        public string SessionId { get; set; } = null!;

        [MaxLength(100)]
        public string? ConversationId { get; set; }

        [MaxLength(100)]
        public string? AgentId { get; set; }

        [MaxLength(100)]
        public string? UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string TenantId { get; set; } = null!;

       
        public Dictionary<string,object>? Metadata { get; set; }

       
    }

}
