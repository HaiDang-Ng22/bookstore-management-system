using System.Net;
using System.Net.Mail;
using System.Text;

namespace BookStoreOnline.Common
{
    public static class EmailHelper
    {
        public static bool SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                // Cấu hình thông tin người gửi (Thay bằng Gmail của bạn)
                string fromEmail = "vumanh0366@gmail.com";
                string password = "geco gdhk acux babc"; // Mật khẩu ứng dụng Gmail (App Password), không phải mật khẩu chính

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(fromEmail, "Nhà Sách BookStoreOnline");
                mail.To.Add(new MailAddress(toEmail));
                mail.Subject = subject;
                mail.Body = body;
                mail.IsBodyHtml = true; // Cho phép viết code HTML cho email đẹp hơn
                mail.BodyEncoding = Encoding.UTF8;

                using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.EnableSsl = true;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(fromEmail, password);
                    smtp.Send(mail);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}