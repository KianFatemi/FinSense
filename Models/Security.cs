using System.ComponentModel.DataAnnotations.Schema;

namespace PersonalFinanceDashboard.Models
{
    public class Security
    {
        public int ID { get; set; }
        public string? PlaidSecurityId { get; set; }
        public string? TickerSymbol { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }

        [Column(TypeName = "decimal(18, 4)")]
        public decimal? ClosePrice { get; set; }
        public DateTime? ClosePriceAsOf { get; set; }
        public ICollection<Holding> Holdings { get; set; } = new List<Holding>();
    }

}

