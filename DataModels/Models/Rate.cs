using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Models;

[Table("rates")]
public class Rate
{
    [Column("id")]
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = null!;

    [Column("status")]
    public string? Status { get; set; }

    [Column("end_date")]
    public DateTime EndDate { get; set; }

    [Column("left_days")]
    public int LeftDays { get; set; }

    [Column("paid_days")]
    public int PaidDays { get; set; }

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("paid_by_date")]
    public DateTime? PaidByDate { get; set; }

    [Column("left_paid_days")]
    public int LeftPaidDays { get; set; }

    [Column("early_termination_date")]
    public DateTime? EarlyTerminationDate { get; set; }
}
