using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace APPLE_STORE.Models
{
    public class APPLE_STORE
    {
        // DbSet cho bảng TaiKhoan (Tài khoản)
        public DbSet<TaiKhoan> TaiKhoan { get; set; }

        // DbSet cho bảng DanhMuc (Danh mục sản phẩm)
        public DbSet<DanhMuc> DanhMuc { get; set; } 

        // DbSet cho bảng SanPham (Sản phẩm)
        public DbSet<SanPham> SanPham { get; set; }

        // DbSet cho bảng ChiTietDonHang (Chi tiết đơn hàng)
        public DbSet<ChiTietDonHang> ChiTietDonHang { get; set; }

        // DbSet cho bảng DonHang (Đơn hàng)
        public DbSet<DonHang> DonHang { get; set; }

        // DbSet cho bảng GioHang (Giỏ hàng)
        public DbSet<GioHang> GioHang { get; set; }

         public DbSet<ThanhToan> ThanhToan { get; set; }
         public DbSet<DanhGia> DanhGia { get; set; }

    }
}