using Shared.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Domain.Entities
{
    public class ConversationRating:AuditableEntity
    {
       
        public Guid ConversationId { get; set; }
        public int Rating { get; set; }
        public string Feedback { get; set; }
        public string Categories { get; set; }
        public string RatedBy { get; set; }
      public Conversation Conversation { get; set; }
    }
}
