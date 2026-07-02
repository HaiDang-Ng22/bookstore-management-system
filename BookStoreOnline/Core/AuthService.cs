using System;
using System.Linq;
using BookStoreOnline.Models;
using static BookStoreOnline.Areas.Admin.Constants.Constants;

namespace BookStoreOnline.Core
{
    public class AuthService
    {
        private readonly NhaSachEntities3 _db;
        private readonly RoleService _roleService;

        public AuthService(NhaSachEntities3 db)
        {
            _db = db;
            _roleService = new RoleService(db);
        }

        public NHANVIEN TryStaffLogin(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return null;

            _roleService.EnsureRoleTableExists();

            var staff = _db.Database.SqlQuery<StaffLoginRow>(@"
                SELECT MaNV, Email, MatKhau, Quyen, Ten, NgayTao, TrangThai
                FROM NHANVIEN
                WHERE Email = @p0 AND MatKhau = @p1", email.Trim(), password).FirstOrDefault();

            if (staff == null) return null;

            return new NHANVIEN
            {
                MaNV = staff.MaNV,
                Email = staff.Email,
                MatKhau = staff.MatKhau,
                Quyen = staff.Quyen,
                Ten = staff.Ten,
                NgayTao = staff.NgayTao,
                TrangThai = staff.TrangThai
            };
        }

        public string GetRedirectUrl(NHANVIEN staff)
        {
            if (staff.Quyen == (int)AdminRole.Shipper)
                return "~/Shipper/OrdersShipper";

            if (staff.Quyen == (int)AdminRole.Admin)
                return "~/Admin/Home_Page";

            return null;
        }

        private class StaffLoginRow
        {
            public int MaNV { get; set; }
            public string Email { get; set; }
            public string MatKhau { get; set; }
            public int Quyen { get; set; }
            public string Ten { get; set; }
            public DateTime? NgayTao { get; set; }
            public bool? TrangThai { get; set; }
        }
    }
}
