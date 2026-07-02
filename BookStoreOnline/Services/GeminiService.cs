using Newtonsoft.Json;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;

namespace BookStoreOnline.Services
{
    public class GeminiService
    {
        private readonly string apiKey;
        private readonly string model;

        public GeminiService()
        {
            apiKey = ConfigurationManager.AppSettings["GeminiApiKey"];
            model = ConfigurationManager.AppSettings["GeminiModel"];
        }

        public string Ask(string question)
        {
            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text =
@"Bạn là chatbot của website BookStore.

Bạn chỉ hỗ trợ:

- tư vấn sách
- tìm kiếm sách
- thanh toán
- vận chuyển
- đơn hàng

Nếu người dùng hỏi ngoài phạm vi hãy trả lời lịch sự.

Câu hỏi:

" + question
                            }
                        }
                    }
                }
            };

            string json = JsonConvert.SerializeObject(body);

            var request = (HttpWebRequest)WebRequest.Create(
                "https://generativelanguage.googleapis.com/v1beta/models/"
                + model
                + ":generateContent?key="
                + apiKey);

            request.Method = "POST";
            request.ContentType = "application/json";

            using (var stream = request.GetRequestStream())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                stream.Write(bytes, 0, bytes.Length);
            }

            using (var response = request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                string result = reader.ReadToEnd();

                dynamic obj = JsonConvert.DeserializeObject(result);

                return obj.candidates[0].content.parts[0].text.ToString();
            }
        }
    }
}