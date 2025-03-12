using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Invoice_and_Payment_System.Models
{
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string InvoiceNumber { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public AppUser User { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        public DateTime IssueDate { get; set; }
        public DateTime? PaymentDate { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [Required]
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;

        [MaxLength(500)]
        public string Description { get; set; }

        [MaxLength(100)]
        public string PaymentMethod { get; set; }

        [MaxLength(100)]
        public string PaymentReference { get; set; }
        public virtual ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();

        [NotMapped]
        public bool IsOverdue => Status == InvoiceStatus.Pending && DueDate < DateTime.UtcNow;


        public void MarkAsPaid(string paymentMethod, string reference)
        {
            Status = InvoiceStatus.Paid;
            PaymentMethod = paymentMethod;
            PaymentReference = reference;
            PaymentDate = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateStatus(InvoiceStatus newStatus)
        {
            Status = newStatus;
            UpdatedAt = DateTime.UtcNow;
        }

    }
}

public enum InvoiceStatus
{
    Pending,
    Paid,
    Overdue,
    Cancelled,
    Disputed,
    PartiallyPaid
}

