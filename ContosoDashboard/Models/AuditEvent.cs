using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class AuditEvent
{
    [Key]
    public int AuditEventId { get; set; }

    public int? DocumentId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string EventDescription { get; set; } = string.Empty;

    public DateTime EventTimeUtc { get; set; } = DateTime.UtcNow;

    [ForeignKey("DocumentId")]
    public virtual Document? Document { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}
