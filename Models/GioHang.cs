namespace APPLE_STORE.Models
{
    using System;

    public partial class GioHang
    {
        private readonly WebBanHangEntities2 db = new WebBanHangEntities2();

        // Constructor m?c ??nh
        public GioHang() { }

        // Constructor kh?i t?o gi? hàng v?i thông tin s?n ph?m
        public GioHang(int maSP)
        {
            // L?y thông tin s?n ph?m t? c? s? d? li?u
            var sanPham = db.SanPhams.Find(maSP);
            if (sanPham != null)
            {
                MaSanPham = sanPham.Id;
                TenSanPham = sanPham.Ten;
                HinhAnh = sanPham.HinhAnh;
                Gia = (decimal)double.Parse(sanPham.Gia.ToString());
                SoLuong = 1; // M?c ??nh s? l??ng là 1
            }
            else
            {
                throw new Exception("Không tìm th?y s?n ph?m v?i ID: " + maSP);
            }
        }

        public int MaGioHang { get; set; }
        public int MaNguoiDung { get; set; }
        public int MaSanPham { get; set; }
        public string TenSanPham { get; set; }
        public int SoLuong { get; set; }
        public decimal Gia { get; set; }
        public string HinhAnh { get; set; }
        public DateTime? NgayTao { get; set; }
        public string TrangThai { get; set; }

        // Tính thành ti?n
        public decimal dThanhTien
        {
            get { return SoLuong * Gia; }
        }

        // Navigation properties
        public virtual SanPham SanPham { get; set; } // ?? ánh x? FK MaSanPham
        public virtual TaiKhoan TaiKhoan { get; set; }
    }
}