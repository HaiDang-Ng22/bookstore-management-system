using System;
using System.Web.Mvc;
using BookStoreOnline.Services;
using System.Linq;
using BookStoreOnline.Models;

namespace BookStoreOnline.Controllers
{
    public class ChatBotController : Controller
    {
        private readonly GeminiService gemini = new GeminiService();
        private readonly ChatBotService chatService =
        new ChatBotService();
        private NhaSachEntities3 db = new NhaSachEntities3();
        [HttpPost]
        public JsonResult Ask(string message)
        {
            string lower = message.ToLower();

            // ==========================
            // 1. Tìm sách theo tên
            // ==========================
            if (lower.Contains("sách"))
            {
                string keyword = message.Replace("sách", "").Trim();

                var result = chatService.GetBookCard(keyword);

                if (!string.IsNullOrEmpty(result))
                {
                    return Json(new
                    {
                        success = true,
                        isHtml = true,
                        answer = result
                    });
                }
            }

            // ==========================
            // 2. Tìm sách theo giá
            // Ví dụ: sách dưới 100000
            // ==========================
            if (lower.Contains("dưới"))
            {
                var number = System.Text.RegularExpressions.Regex.Match(lower, @"\d+");

                if (number.Success)
                {
                    int price = int.Parse(number.Value);

                    var result = chatService.GetBookByPrice(price);

                    if (!string.IsNullOrEmpty(result))
                    {
                        return Json(new
                        {
                            success = true,
                            isHtml = false,
                            answer = result
                        });
                    }
                }
            }

            // ==========================
            // 3. Top bán chạy
            // ==========================
            if (lower.Contains("bán chạy")
                || lower.Contains("best seller")
                || lower.Contains("top"))
            {
                return Json(new
                {
                    success = true,
                    isHtml = false,
                    answer = chatService.GetBestSeller()
                });
            }

            // ==========================
            // 4. Tra cứu đơn hàng
            // Ví dụ:
            // đơn hàng 15
            // ==========================
            if (lower.Contains("đơn hàng"))
            {
                var number = System.Text.RegularExpressions.Regex.Match(lower, @"\d+");

                if (number.Success)
                {
                    return Json(new
                    {
                        success = true,
                        isHtml = false,
                        answer = chatService.GetOrder(int.Parse(number.Value))
                    });
                }
            }

            // ==========================
            // 5. Gợi ý theo thể loại
            // ==========================
            foreach (var loai in db.LOAIs.ToList())
            {
                if (lower.Contains(loai.Tenloai.ToLower()))
                {
                    return Json(new
                    {
                        success = true,
                        isHtml = false,
                        answer = chatService.GetBookByCategory(loai.Tenloai)
                    });
                }
            }

            // ==========================
            // 6. Không tìm thấy -> Gemini
            // ==========================
            string answer = gemini.Ask(message);

            return Json(new
            {
                success = true,
                isHtml = false,
                answer = answer
            });
        }
    }
}