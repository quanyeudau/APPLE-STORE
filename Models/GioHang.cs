namespace APPLE_STORE.Models
{
    using System;

    public partial class GioHang
    {
        private readonly WebBanHangEntities2 db = new WebBanHangEntities2();

        // Constructor m?c ??nh
        public GioHang() { }

        // Constructor kh?i t?o gi? h�ng v?i th�ng tin s?n ph?m
        public GioHang(int maSP)
        {
            // L?y th�ng tin s?n ph?m t? c? s? d? li?u
            var sanPham = db.SanPhams.Find(maSP);
            if (sanPham != null)
            {
                MaSanPham = sanPham.Id;
                TenSanPham = sanPham.Ten;
                HinhAnh = sanPham.HinhAnh;
                Gia = (decimal)double.Parse(sanPham.Gia.ToString());
                SoLuong = 1; // M?c ??nh s? l??ng l� 1
            }
            else
            {
                throw new Exception("Kh�ng t�m th?y s?n ph?m v?i ID: " + maSP);
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

        // T�nh th�nh ti?n
        public decimal dThanhTien
        {
            get { return SoLuong * Gia; }
        }

        // Navigation properties
        public virtual SanPham SanPham { get; set; } // ?? �nh x? FK MaSanPham
        public virtual TaiKhoan TaiKhoan { get; set; }
    }
}