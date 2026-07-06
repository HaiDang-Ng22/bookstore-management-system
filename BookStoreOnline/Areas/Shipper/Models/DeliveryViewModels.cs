using System;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace BookStoreOnline.Areas.Shipper.Models
{
    public enum DeliveryFailureReason
    {
        [Display(Name = "Khách không nghe máy")] NoAnswer = 1,
        [Display(Name = "Khách hẹn giao lại")] CustomerRescheduled = 2,
        [Display(Name = "Sai địa chỉ")] WrongAddress = 3,
        [Display(Name = "Khách từ chối nhận")] Refused = 4,
        [Display(Name = "Không đủ tiền thanh toán COD")] InsufficientCod = 5,
        [Display(Name = "Hàng bị lỗi hoặc hư hỏng")] Damaged = 6,
        [Display(Name = "Không thể liên hệ khách hàng")] CannotContact = 7
    }

    public class DeliveryFailureViewModel
    {
        public int OrderId { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn lý do giao thất bại.")]
        public DeliveryFailureReason? Reason { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập ghi chú.")]
        [StringLength(1000)] public string Note { get; set; }
        public bool Reschedule { get; set; }
        public DateTime? RescheduledAt { get; set; }
        public HttpPostedFileBase EvidenceImage { get; set; }
    }

    public class DeliverySuccessViewModel
    {
        public int OrderId { get; set; }
        [StringLength(1000)] public string Note { get; set; }
        public HttpPostedFileBase EvidenceImage { get; set; }
    }

    public class DeliveryHistoryItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int Status { get; set; }
        public string Reason { get; set; }
        public string Note { get; set; }
        public string EvidenceImage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RescheduledAt { get; set; }
    }
}
