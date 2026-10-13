using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using WebApplication1.Models;


namespace WebApplication1.Controllers // 👈 與你的專案名稱保持一致
{
    public class HomeController : Controller
    {
        private readonly string _connectionString;

        public HomeController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // 【第一面】選場次：列出所有演唱會
        public IActionResult Index()
        {
            DataTable dt = new DataTable();
            string sqlQuery = "SELECT * FROM Concerts;";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(sqlQuery, conn))
                {
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) { da.Fill(dt); }
                }
            }
            return View(dt);
        }

        // 【第二面】選區域：根據選定的 ConcertID，並動態計算出「剩餘票數」
        public IActionResult SelectZone(string concertId)
        {
            DataTable dt = new DataTable();

            // 👈 核心升級：使用子查詢，即時計算 RemainingSeats (總座位 - 已賣出明細數量)
            string sqlQuery = @"
                SELECT 
                    ZoneID, 
                    ConcertID, 
                    ZoneName, 
                    Price, 
                    TotalSeats,
                    (TotalSeats - (SELECT COUNT(*) FROM OrderDetails od WHERE od.ZoneID = cz.ZoneID)) AS RemainingSeats
                FROM ConcertZones cz
                WHERE ConcertID = @ConcertID;";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(sqlQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@ConcertID", concertId);
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) { da.Fill(dt); }
                }
            }
            ViewBag.ConcertID = concertId;
            return View(dt);
        }

        // 【第三面】實名制與驗證碼填寫
        public IActionResult Checkout(int zoneId, string concertId)
        {
            // 檢查 Cookie 是否存在
            string memberId = Request.Cookies["MemberID"];

            // 👈 刪除原本檢查 Session 的 if，只保留檢查 Cookie 的即可
            if (string.IsNullOrEmpty(memberId))
            {
                TempData["Error"] = "搶票前請先登入會員！";
                return RedirectToAction("Login");
            }

            // 隨機產生一個 4 碼驗證碼供前端顯示
            Random rand = new Random();
            string captcha = rand.Next(1000, 9999).ToString();

            ViewBag.ZoneID = zoneId;
            ViewBag.ConcertID = concertId;
            ViewBag.Captcha = captcha;

            return View();
        }

        // 【送出搶票】處理實名制訂單寫入資料庫（內建防超賣、防完售安全機制）
        [HttpPost]
        public IActionResult SubmitOrder(string concertId, int zoneId, int totalTickets, string realName, string idNumber, string userCaptcha, string correctCaptcha)
        {
            string memberId = Request.Cookies["MemberID"]; // 替換掉原本的 Session 抓取
            if (string.IsNullOrEmpty(memberId)) return RedirectToAction("Login");

            // 1. 驗證碼檢查
            if (userCaptcha != correctCaptcha)
            {
                TempData["Error"] = "驗證碼輸入錯誤，搶票失敗！";
                return RedirectToAction("Checkout", new { zoneId = zoneId, concertId = concertId });
            }

            // 2. 拓元限購檢查
            if (totalTickets > 4 || totalTickets <= 0)
            {
                TempData["Error"] = "每人限購 1~4 張門票！";
                return RedirectToAction("Checkout", new { zoneId = zoneId, concertId = concertId });
            }

            string orderId = "ORD" + DateTime.Now.ToString("yyyyMMddHHmmss");

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // 3. 👈 防超賣檢查：送出前，再次即時查核資料庫當前的賸餘張數
                string checkSeatsSql = @"
                    SELECT (TotalSeats - (SELECT COUNT(*) FROM OrderDetails od WHERE od.ZoneID = cz.ZoneID)) AS RemainingSeats 
                    FROM ConcertZones cz WHERE ZoneID = @ZoneID;";

                int remainingSeats = 0;
                using (SqlCommand checkCmd = new SqlCommand(checkSeatsSql, conn))
                {
                    checkCmd.Parameters.AddWithValue("@ZoneID", zoneId);
                    remainingSeats = Convert.ToInt32(checkCmd.ExecuteScalar());
                }

                if (totalTickets > remainingSeats)
                {
                    TempData["Error"] = $"搶票失敗！該區域票數不足（僅剩 {remainingSeats} 張），已被其他粉絲搶先一步！";
                    return RedirectToAction("SelectZone", new { concertId = concertId });
                }

                // 4. 開啟交易機制，確保主檔跟明細都有成功寫入
                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    try
                    {
                        // 寫入 Orders 主檔
                        string insertOrder = "INSERT INTO Orders (OrderID, MemberID, TotalTickets, OrderDate) VALUES (@OrderID, @MemberID, @TotalTickets, @OrderDate);";
                        using (SqlCommand cmd = new SqlCommand(insertOrder, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@OrderID", orderId);
                            cmd.Parameters.AddWithValue("@MemberID", memberId);
                            cmd.Parameters.AddWithValue("@TotalTickets", totalTickets);
                            cmd.Parameters.AddWithValue("@OrderDate", DateTime.Now);
                            cmd.ExecuteNonQuery();
                        }

                        // 寫入 OrderDetails 實名制明細
                        string insertDetail = "INSERT INTO OrderDetails (OrderID, ZoneID, SeatNumber, RealName, IDNumber) VALUES (@OrderID, @ZoneID, @SeatNumber, @RealName, @IDNumber);";
                        for (int i = 1; i <= totalTickets; i++)
                        {
                            using (SqlCommand cmd = new SqlCommand(insertDetail, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@OrderID", orderId);
                                cmd.Parameters.AddWithValue("@ZoneID", zoneId);
                                cmd.Parameters.AddWithValue("@SeatNumber", "隨機排" + i + "號");
                                cmd.Parameters.AddWithValue("@RealName", realName + (totalTickets > 1 ? $" (同伴{i})" : ""));
                                cmd.Parameters.AddWithValue("@IDNumber", idNumber);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        trans.Commit();
                        TempData["Success"] = $"恭喜你！搶票成功！您的訂單編號為：{orderId}";
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        TempData["Error"] = "系統繁忙或資料庫寫入失敗：" + ex.Message;
                    }
                }
            }

            return RedirectToAction("Index");
        }

        public IActionResult OrderHistory(string searchIdNumber) // 接收前端傳來的參數
        {
            string memberId = Request.Cookies["MemberID"];
            if (string.IsNullOrEmpty(memberId))
            {
                TempData["Error"] = "請先登入以查看購票紀錄。";
                return RedirectToAction("Login");
            }

            DataTable dt = new DataTable();

            // 儲存查詢關鍵字供 View 顯示
            ViewBag.SearchKey = searchIdNumber;

            // 核心查詢邏輯：
            // 如果有輸入身分證，就用身分證查；沒輸入則用會員 ID 查
            string sqlQuery = @"
        SELECT  
            o.OrderID AS 訂單編號,
            c.ArtistName AS 演唱會,
            cz.ZoneName AS 票券區域,
            cz.Price AS 票價,
            od.SeatNumber AS 座位號碼,
            od.RealName AS 持票人姓名,
            o.OrderDate AS 購票時間
        FROM OrderDetails od
        JOIN Orders o ON od.OrderID = o.OrderID
        JOIN ConcertZones cz ON od.ZoneID = cz.ZoneID
        JOIN Concerts c ON cz.ConcertID = c.ConcertID
        WHERE (@SearchID IS NULL OR od.IDNumber = @SearchID)
          AND (@SearchID IS NOT NULL OR o.MemberID = @MemberID)
        ORDER BY o.OrderDate DESC;";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(sqlQuery, conn))
                {
                    // 如果輸入為空，搜尋條件設為 DBNull
                    cmd.Parameters.AddWithValue("@SearchID", (object)searchIdNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MemberID", memberId);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd)) { da.Fill(dt); }
                }
            }

            return View(dt);
        }

        // 【畫面】登入與註冊頁面
        public IActionResult Login()
        {
            return View();
        }

        // 【功能】處理註冊 (雙斜線註冊已修正)
        [HttpPost]
        public IActionResult RegisterUser(string memberId, string memberName, string password)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string checkSql = "SELECT COUNT(*) FROM Members WHERE MemberID = @MemberID;";
                using (SqlCommand checkCmd = new SqlCommand(checkSql, conn))
                {
                    checkCmd.Parameters.AddWithValue("@MemberID", memberId.Trim());
                    if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0)
                    {
                        TempData["Error"] = "該會員帳號已被註冊！";
                        return RedirectToAction("Login");
                    }
                }

                // 👈 C# 註冊批註已修正為雙斜線
                string insertSql = "INSERT INTO Members (MemberID, MemberName, Password) VALUES (@MemberID, @MemberName, @Password);";
                using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                {
                    cmd.Parameters.AddWithValue("@MemberID", memberId.Trim());
                    cmd.Parameters.AddWithValue("@MemberName", memberName.Trim());
                    cmd.Parameters.AddWithValue("@Password", password.Trim());
                    cmd.ExecuteNonQuery();
                }
            }
            TempData["Success"] = "註冊成功！請輸入帳號密碼登入。";
            return RedirectToAction("Login");
        }

        // 【功能】處理登入 (安全比對)
        [HttpPost]
        public IActionResult LoginUser(string memberId, string password)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string sql = "SELECT MemberName, Password FROM Members WHERE MemberID = @MemberID;";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@MemberID", memberId.Trim());
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (reader["Password"].ToString() == password.Trim())
                            {
                                // 👈 核心修改：使用 Cookie 儲存，並設定 7 天內有效
                                CookieOptions options = new CookieOptions();
                                options.Expires = DateTime.Now.AddDays(7);

                                Response.Cookies.Append("MemberID", memberId.Trim(), options);
                                Response.Cookies.Append("MemberName", reader["MemberName"].ToString(), options);

                                return RedirectToAction("Index");
                            }
                        }
                    }
                }
            }
            TempData["Error"] = "帳號或密碼錯誤。";
            return RedirectToAction("Login");
        }

        // 【功能】處理登出
        public IActionResult Logout()
        {
            // 👈 刪除這兩個 Cookie
            Response.Cookies.Delete("MemberID");
            Response.Cookies.Delete("MemberName");
            return RedirectToAction("Index");
        }
        // ===================================================================
        // 👈 期末報告高分必殺技：三合一售票監控面板 (Dashboard)
        // 同時融合了：1. JOIN (串聯4表)  2. Subquery (子查詢)  3. Aggregate (COUNT函數)
        // ===================================================================
        public IActionResult Dashboard()
        {
            List<ZoneStatusViewModel> reportList = new List<ZoneStatusViewModel>();

            // 完美的「三合一進階優化 SQL」
            string sqlQuery = @"
                SELECT 
                    c.ArtistName,
                    cz.ZoneName,
                    cz.Price,
                    cz.TotalSeats,
                    (SELECT COUNT(*) FROM OrderDetails od WHERE od.ZoneID = cz.ZoneID) AS SoldTickets,
                    (cz.TotalSeats - (SELECT COUNT(*) FROM OrderDetails od WHERE od.ZoneID = cz.ZoneID)) AS RemainingSeats
                FROM ConcertZones cz
                JOIN Concerts c ON cz.ConcertID = c.ConcertID
                ORDER BY c.ConcertID, cz.Price DESC;";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(sqlQuery, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new ZoneStatusViewModel
                            {
                                ArtistName = reader["ArtistName"].ToString(),
                                ZoneName = reader["ZoneName"].ToString(),
                                Price = Convert.ToInt32(reader["Price"]),
                                TotalSeats = Convert.ToInt32(reader["TotalSeats"]),
                                SoldTickets = Convert.ToInt32(reader["SoldTickets"]),
                                RemainingSeats = Convert.ToInt32(reader["RemainingSeats"])
                            };
                            reportList.Add(item);
                        }
                    }
                }
            }

            // 將模型資料回傳給專屬的 View 畫面渲染
            return View(reportList);
        }
    }
}