using BookStoreOnline.Core;
using BookStoreOnline.Models;

namespace BookStoreOnline.ViewModels
{
    public class CustomerVIPViewModel
    {
        public KHACHHANG Customer { get; set; }

        public VIPBenefits VIPBenefits { get; set; }
    }
}