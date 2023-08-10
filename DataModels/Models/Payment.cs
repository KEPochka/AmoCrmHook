using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models;

[Table("payments")]
public partial class Payment
{
    [Column("id")]
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Int64 Id { get; set; }

    [Column("price")]
    public decimal Price { get; set; }

    [Column("comment")]
    public string? Comment { get; set; }

    [Column("rate_id")]
    public Int64? RateId { get; set; }

    [Column("event_date")]
    public DateTime? EventDate { get; set; }

    [Column("event_type")]
    public string? EventType { get; set; }

    [Column("actual_date")]
    public DateTime? ActualDate { get; set; }

    [Column("created_date")]
    public DateTime? CreatedDate { get; set; }

    [ForeignKey("PaymentType")]
    [Column("payment_type_id")]
    public Int64? PaymentTypeId { get; set; }

    public PaymentType? PaymentType { get; set; }

    [Column("invoice_number")]
    public string? InvoiceNumber { get; set; }

    [Column("scheduled_date")]
    public DateTime? ScheduledDate { get; set; }

    [Column("contract_number")]
    public string? ContractNumber { get; set; }
}
