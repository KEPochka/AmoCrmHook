using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models;

[Table("payment_types")]
public class PaymentType
{
    [Column("id")]
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Column("value")]
    public string Value { get; set; } = null!;
}
