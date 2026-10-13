
namespace WebApplication1.Models // 👈 請記得改成你的專案命名空間
{
    public class ZoneStatusViewModel
    {
        public string ArtistName { get; set; }     // 演唱會名稱
        public string ZoneName { get; set; }       // 區域名稱
        public int Price { get; set; }             // 票價
        public int TotalSeats { get; set; }        // 總座位數
        public int SoldTickets { get; set; }       // 已售出張數
        public int RemainingSeats { get; set; }    // 剩餘座位數
    }
}