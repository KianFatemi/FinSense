using Microsoft.AspNetCore.Identity;
namespace PersonalFinanceDashboard.Models
{
    public class PlaidItem
    {
        public int ID { get; set; }
        public string? UserID { get; set; }
        public IdentityUser? User { get; set; }
        public string? AccessToken { get; set; }
        public string? ItemID { get; set; }
        public string? LastSyncCursor { get; set; }
    }
}
