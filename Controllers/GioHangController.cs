using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web;
using System.Web.Mvc;
using APPLE_STORE.Models;

namespace APPLE_STORE.Controllers
{
    public class GioHangController : Controller
    {
        private WebBanHangEntities2 db = new WebBanHangEntities2();

        // GET: GioHang
        [HttpGet]
        public ActionResult DatHang()
        {
            // Kiểm tra nếu chưa đăng nhập
            if (Session["User"] == null)
            {
                return RedirectToAction("Login", "Home");
            }

            // Kiểm tra nếu giỏ hàng trống
            if (Session["GioHang"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Lấy giỏ hàng từ session
            List<GioHang> lstGioHang = LayGioHang();
            ViewBag.TongSoLuong = TongSoLuong();
            ViewBag.TongTien = TongTien();

            // Lấy thông tin khách hàng từ session
            var kh = Session["User"] as TaiKhoan;
            if (kh != null)
            {
                ViewBag.TaiKhoan = kh;
            }

            return View(lstGioHang);
        }

        [HttpPost]
        public ActionResult DatHang(string HoTen, string DiaChi, string DienThoai, string NgayGiao, string thanhtoan)
        {
            // Lấy giỏ hàng từ session
            List<GioHang> lstCart = LayGioHang();
            if (lstCart == null || !lstCart.Any())
            {
                return RedirectToAction("Index", "Home");
            }

            // Cập nhật thông tin khách hàng
            var kh = Session["User"] as TaiKhoan;
            if (kh != null)
            {
                using (var dbContext = new WebBanHangEntities2()) // Thay bằng tên DbContext của bạn
                {
                    // Lấy khách hàng từ cơ sở dữ liệu để đảm bảo đối tượng chỉ được quản lý bởi một DbContext
                    var TaiKhoanInDb = dbContext.TaiKhoans.SingleOrDefault(k => k.IdTaiKhoan == kh.IdTaiKhoan);
                    if (TaiKhoanInDb != null)
                    {
                        TaiKhoanInDb.HoTen = HoTen;
                        TaiKhoanInDb.DiaChi = DiaChi;
                        TaiKhoanInDb.SoDienThoai = DienThoai;

                        // Đánh dấu thực thể đã sửa đổi
                        dbContext.Entry(TaiKhoanInDb).State = EntityState.Modified;
                        dbContext.SaveChanges();

                        // Cập nhật lại session
                        Session["User"] = TaiKhoanInDb;
                    }
                }
            }

            // Xử lý thanh toán
            switch (thanhtoan)
            {
                case "vivnpay":
                    return RedirectToAction("PaymentVNPay", "GioHang", new { NgayGiao });

                case "vimomo":
                    return RedirectToAction("PaymentMomo", "GioHang");

                case "nhanhang":
                    // Lưu đơn hàng vào cơ sở dữ liệu và nhận đối tượng đơn hàng (ddh)
                    var ddh = LuuDonHang(NgayGiao, false);  // Lưu đơn hàng và lấy đối tượng đơn hàng (ddh)

                    // Gửi email xác nhận đơn hàng
                    XacNhanDonHang(ddh, kh, lstCart);

                    // Chuyển hướng đến trang xác nhận đơn hàng
                    return RedirectToAction("ConfirmOrder", "GioHang");

                default:
                    ModelState.AddModelError("", "Hình thức thanh toán không hợp lệ!");
                    break;
            }

            // Trả về giỏ hàng nếu có lỗi
            ViewBag.TongSoLuong = TongSoLuong();
            ViewBag.TongTien = TongTien();
            ViewBag.TaiKhoan = kh;
            return View(lstCart);
        }


        public ActionResult GioHang()
        {
            List<GioHang> lstGioHang = LayGioHang();
            if (lstGioHang.Count == 0)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.TongSoLuong = TongSoLuong();
            ViewBag.TongTien = TongTien();
            return View(lstGioHang);
        }

        public ActionResult CapNhatGioHang(int MaSanPham, FormCollection f)
        {
            List<GioHang> lstGioHang = LayGioHang();
            GioHang sp = lstGioHang.SingleOrDefault(n => n.MaSanPham == MaSanPham);
            if (sp != null)
            {
                sp.SoLuong = int.Parse(f["txtSoLuong"].ToString());
            }
            return RedirectToAction("GioHang");
        }

        public ActionResult XoaGioHang()
        {
            // Xóa toàn bộ giỏ hàng
            Session["GioHang"] = null;

            // Quay lại trang chủ hoặc trang giỏ hàng
            return RedirectToAction("Index", "Home");
        }

        public ActionResult XoaSPKhoiGioHang(int MaSanPham)
        {
            // Lấy giỏ hàng từ Session
            List<GioHang> lstGioHang = LayGioHang();

            // Kiểm tra sản phẩm trong giỏ hàng
            GioHang sp = lstGioHang.SingleOrDefault(n => n.MaSanPham == MaSanPham);
            if (sp != null)
            {
                // Xóa sản phẩm khỏi danh sách giỏ hàng
                lstGioHang.Remove(sp);

                // Cập nhật lại giỏ hàng trong Session
                Session["GioHang"] = lstGioHang;

                // Nếu giỏ hàng trống, chuyển về trang chủ
                if (lstGioHang.Count == 0)
                {
                    Session["GioHang"] = null; // Đặt lại Session nếu không còn sản phẩm
                    return RedirectToAction("Index", "Home");
                }

                // Cập nhật lại tổng số lượng và tổng tiền
                ViewBag.TongSoLuong = lstGioHang.Sum(x => x.SoLuong);
                ViewBag.TongTien = lstGioHang.Sum(x => (int)x.dThanhTien);
            }
            else
            {
                // Nếu sản phẩm không tồn tại trong giỏ hàng
                ViewBag.ErrorMessage = "Sản phẩm không tồn tại trong giỏ hàng.";
            }

            // Quay lại trang Giỏ Hàng
            return RedirectToAction("GioHang");
        }


        public ActionResult ThemGioHang(int id, int? quantity, string url)
        {
            try
            {
                if (quantity == null || quantity <= 0)
                {
                    quantity = 1;
                }

                List<GioHang> lstCart = LayGioHang();
                var sp = lstCart.SingleOrDefault(n => n.MaSanPham == id);

                if (sp == null)
                {
                    sp = new GioHang(id);  // Dùng constructor mới ở trên
                    sp.SoLuong = quantity.Value;
                    lstCart.Add(sp);
                }
                else
                {
                    sp.SoLuong += quantity.Value;
                }

                Session["GioHang"] = lstCart;

                if (string.IsNullOrEmpty(url))
                {
                    url = Url.Action("Index", "Home");
                }

                return Redirect(url);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }


        public List<GioHang> LayGioHang()
        {
            if (Session["GioHang"] is List<GioHang> lstCart)
                return lstCart;

            lstCart = new List<GioHang>();
            Session["GioHang"] = lstCart;
            return lstCart;
        }


        private int TongSoLuong()
        {
            int iTongSoLuong = 0;
            List<GioHang> lstGioHang = Session["GioHang"] as List<GioHang>;
            if (lstGioHang != null)
            {
                iTongSoLuong = lstGioHang.Sum(n => n.SoLuong);
            }
            return iTongSoLuong;
        }

        private double TongTien()
        {
            double dTongTien = 0;
            List<GioHang> lstGioHang = Session["GioHang"] as List<GioHang>;
            if (lstGioHang != null)
            {
                dTongTien = (double)lstGioHang.Sum(n => n.Gia * n.SoLuong);
            }
            return dTongTien;
        }
        public ActionResult XacNhanDonHang()
        {
            // Thực hiện các hành động cần thiết, như lấy dữ liệu đơn hàng hoặc thông báo.
            return View();  // Đảm bảo trả về đúng view của trang xác nhận đơn hàng.
        }

        private void XacNhanDonHang(DonHang ddh, TaiKhoan kh, List<GioHang> gioHang)
        {
            try
            {
                var mail = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential("2224801030198@student.tdmu.edu.vn", "tesm tjuj jsxt agzi"),
                    EnableSsl = true
                };

                // Build the HTML table for order details
                string chiTietDonHang = @"
        <html>
        <head>
            <style>
                table {
                    width: 100%;
                    border-collapse: collapse;
                }
                table, th, td {
                    border: 1px solid black;
                    padding: 8px;
                    text-align: left;
                }
                th {
                    background-color: #f2f2f2;
                }
            </style>
        </head>
        <body>
            <h2>Chi tiết đơn hàng của bạn</h2>
            <table>
                <tr>
                    <th>Tên sách</th>
                    <th>Số lượng</th>
                    <th>Đơn giá</th>
                    <th>Thành tiền</th>
                </tr>";

                // Add each item in the cart
                foreach (var item in gioHang)
                {
                    chiTietDonHang += $@"
            <tr>
                <td>{item.TenSanPham}</td>
                <td>{item.SoLuong}</td>
                <td>{item.Gia:#,##0} VND</td>
                <td>{item.dThanhTien:#,##0} VND</td>
            </tr>";
                }

                chiTietDonHang += $@"
        <tr>
            <td colspan='3' style='text-align: right; font-weight: bold;'>Tổng tiền</td>
            <td style='font-weight: bold;'>{TotalAmount():#,##0} VND</td>
        </tr>
        </table>
        <br>
        <p>Thông tin cá nhân của bạn:</p>
        <ul>
            <li><strong>Họ tên:</strong> {kh.HoTen}</li>
            <li><strong>Địa chỉ:</strong> {kh.DiaChi}</li>
            <li><strong>Số điện thoại:</strong> {kh.SoDienThoai}</li>
        </ul>
        <p>Chúng tôi sẽ xử lý đơn hàng của bạn trong thời gian sớm nhất.</p>
        <br>
        <p>Trân trọng,</p>
        <p>Đội ngũ hỗ trợ Shop ĐẶNG ANH QUÂN</p>
        </body>
        </html>";

                var message = new MailMessage
                {
                    From = new MailAddress("2224801030198@student.tdmu.edu.vn"),
                    Subject = "Xác nhận đơn hàng từ Shop ",
                    Body = chiTietDonHang,
                    IsBodyHtml = true // Ensure the email body is interpreted as HTML
                };

                message.To.Add(new MailAddress(kh.Email)); // Use dynamic email
                mail.Send(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Đã xảy ra lỗi khi gửi email: " + ex.Message);
            }
        }

        private DonHang LuuDonHang(string NgayGiao, bool daThanhToan)
        {
            try
            {
                // Lấy thông tin khách hàng từ Session
                TaiKhoan kh = (TaiKhoan)Session["User"];
                if (kh == null)
                {
                    throw new Exception("Người dùng chưa đăng nhập.");
                }

                // Kiểm tra và lấy giỏ hàng
                List<GioHang> lstCart = LayGioHang();
                if (lstCart == null || !lstCart.Any())
                {
                    throw new Exception("Giỏ hàng đang trống.");
                }

                // Lấy thông tin địa chỉ, số điện thoại, và họ tên từ Session
                string DienThoai = (string)Session["DienThoai"];
                string DiaChi = (string)Session["DiaChi"];
                string HoTen = (string)Session["HoTen"];

                // Kiểm tra các giá trị Session
                if (string.IsNullOrEmpty(DienThoai))
                    throw new Exception("Số điện thoại chưa được lưu vào Session.");
                if (string.IsNullOrEmpty(DiaChi))
                    throw new Exception("Địa chỉ giao hàng chưa được lưu vào Session.");
                if (string.IsNullOrEmpty(HoTen))
                    throw new Exception("Họ tên khách hàng chưa được lưu vào Session.");

                // Tính tổng giá trị đơn hàng
                decimal TongGia = lstCart.Sum(item => item.SoLuong * item.Gia);

                // Tạo đối tượng đơn hàng mới
                DonHang ddh = new DonHang
                {
                    MaDonHang = "DH" + DateTime.Now.Ticks.ToString(), // Mã đơn hàng được tạo từ ticks của thời gian
                    NgayDat = DateTime.Now,
                    NgayGiao = DateTime.Parse(NgayGiao),
                    DiaChiGiaoHang = DiaChi,
                    SoDienThoaiGiao = DienThoai,
                    TenNguoiNhan = HoTen,
                    TrangThai = "Chờ Xử Lý",
                    TrangThaiThanhToan = daThanhToan ? "Đã Thanh Toán" : "Chưa Thanh Toán",
                    TongGia = TongGia
                };

                // Thêm đơn hàng vào cơ sở dữ liệu
                db.DonHangs.Add(ddh);
                db.SaveChanges();  // Lưu vào cơ sở dữ liệu

                // Thêm chi tiết đơn hàng
                foreach (var item in lstCart)
                {
                    ChiTietDonHang ctdh = new ChiTietDonHang
                    {
                        IdDonHang = ddh.Id,
                        IdSanPham = item.MaSanPham,
                        SoLuong = item.SoLuong,
                        DonGia = item.Gia,
                        HinhAnh = item.HinhAnh,
                        ThanhTien = item.SoLuong * item.Gia
                    };

                    db.ChiTietDonHangs.Add(ctdh); // Thêm chi tiết vào cơ sở dữ liệu
                }

                // Lưu chi tiết đơn hàng
                db.SaveChanges();

                // Xác nhận đơn hàng đã được tạo
                XacNhanDonHang(ddh, kh, lstCart);

                // Xóa giỏ hàng sau khi đặt hàng thành công
                Session["GioHang"] = null;

                // Thông báo thành công
                return ddh; // Trả về đối tượng đơn hàng vừa tạo
            }
            catch (Exception ex)
            {
                // Ghi log lỗi hoặc hiển thị thông báo lỗi
                Response.Write("Có lỗi xảy ra: " + ex.Message);
                return null; // Trả về null nếu có lỗi
            }
        }

        public ActionResult PaymentConfirm()
        {
            if (Request.QueryString.Count > 0)
            {
                string hashSecret = ConfigurationManager.AppSettings["HashSecret"]; // Khóa bí mật dùng để kiểm tra chữ ký
                var vnpayData = Request.QueryString;

                PayLib pay = new PayLib();

                // Lấy toàn bộ dữ liệu trả về từ VNPay
                foreach (string s in vnpayData)
                {
                    if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                    {
                        pay.AddResponseData(s, vnpayData[s]);
                    }
                }

                long orderId = Convert.ToInt64(pay.GetResponseData("vnp_TxnRef")); // Mã hóa đơn
                long vnpayTranId = Convert.ToInt64(pay.GetResponseData("vnp_TransactionNo")); // Mã giao dịch tại hệ thống VNPay
                string vnp_ResponseCode = pay.GetResponseData("vnp_ResponseCode"); // Mã phản hồi (00: thành công)
                string vnp_SecureHash = Request.QueryString["vnp_SecureHash"]; // Chữ ký dữ liệu trả về

                // Kiểm tra tính hợp lệ của chữ ký
                bool checkSignature = pay.ValidateSignature(vnp_SecureHash, hashSecret);

                if (checkSignature)
                {
                    if (vnp_ResponseCode == "00")
                    {
                        // Giao dịch thanh toán thành công
                        ViewBag.Message = "Thanh toán thành công hóa đơn " + orderId + " | Mã giao dịch: " + vnpayTranId;

                        // Gửi email xác nhận
                        string email = "2224801030198@student.tdmu.edu.vn"; // Thay bằng email của khách hàng
                        SendEmail(email, "Xác nhận thanh toán", $"Đơn hàng {orderId} đã thanh toán thành công. Mã giao dịch: {vnpayTranId}");
                    }
                    else
                    {
                        // Giao dịch thanh toán không thành công
                        ViewBag.Message = "Có lỗi xảy ra trong quá trình xử lý hóa đơn " + orderId
                            + " | Mã giao dịch: " + vnpayTranId + " | Mã lỗi: " + vnp_ResponseCode;
                    }
                }
                else
                {
                    // Chữ ký không hợp lệ
                    ViewBag.Message = "Có lỗi xảy ra trong quá trình xử lý";
                }
            }

            return View();
        }

        public void SendEmail(string toEmail, string subject, string body)
        {
            string smtpHost = "smtp.gmail.com"; // SMTP server của Gmail
            int smtpPort = 587; // Cổng SMTP của Gmail
            string smtpEmail = "2224801030198@student.tdmu.edu.vn"; // Email của bạn
            string smtpPassword = "dknz bxjh roae amdi"; // Mật khẩu email của bạn

            using (var smtpClient = new SmtpClient(smtpHost, smtpPort))
            {
                smtpClient.Credentials = new System.Net.NetworkCredential(smtpEmail, smtpPassword);
                smtpClient.EnableSsl = true; // Gmail yêu cầu SSL

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpEmail),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                smtpClient.Send(mailMessage);
            }
        }


        public ActionResult PaymentVNPay(int? id, string ngayGiao)
        {
            string url = ConfigurationManager.AppSettings["Url"]; // URL cổng thanh toán VNPay
            string returnUrl = ConfigurationManager.AppSettings["ReturnUrl"]; // URL trả về sau khi thanh toán
            string tmnCode = ConfigurationManager.AppSettings["TmnCode"]; // Mã website của merchant trên hệ thống VNPay
            string hashSecret = ConfigurationManager.AppSettings["HashSecret"]; // Khóa bí mật dùng để tạo chữ ký

            PayLib pay = new PayLib();

            pay.AddRequestData("vnp_Version", "2.1.0"); // Phiên bản API (2.1.0)
            pay.AddRequestData("vnp_Command", "pay"); // Mã giao dịch thanh toán (pay)
            pay.AddRequestData("vnp_TmnCode", tmnCode); // Mã merchant
            pay.AddRequestData("vnp_Amount", TotalAmount().ToString() + "00"); // Số tiền thanh toán
            pay.AddRequestData("vnp_BankCode", "NCB"); // Mã ngân hàng thanh toán (tùy chọn)
            pay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss")); // Ngày tạo giao dịch
            pay.AddRequestData("vnp_CurrCode", "VND"); // Loại tiền tệ
            pay.AddRequestData("vnp_IpAddr", Util.GetIpAddress()); // Địa chỉ IP khách hàng
            pay.AddRequestData("vnp_Locale", "vn"); // Ngôn ngữ hiển thị
            pay.AddRequestData("vnp_OrderInfo", "Thanh toán đơn hàng"); // Nội dung thanh toán
            pay.AddRequestData("vnp_OrderType", "other"); // Loại giao dịch
            pay.AddRequestData("vnp_ReturnUrl", returnUrl); // URL trả về sau khi hoàn tất giao dịch
            pay.AddRequestData("vnp_TxnRef", DateTime.Now.Ticks.ToString()); // Mã hóa đơn

            string paymentUrl = pay.CreateRequestUrl(url, hashSecret); // Tạo URL thanh toán

            LuuDonHang(ngayGiao, true); // Lưu đơn hàng với trạng thái đã thanh toán
            return Redirect(paymentUrl); // Chuyển hướng đến cổng thanh toán
        }
        private decimal TotalAmount()
        {
            decimal totalAmount = 0;
            List<GioHang> lstGioHang = Session["GioHang"] as List<GioHang>;

            if (lstGioHang != null)
            {
                totalAmount = lstGioHang.Sum(item => (decimal)item.dThanhTien);
            }

            return totalAmount;
        }

        public ActionResult ConfirmOrder()
        {
            // Kiểm tra nếu giỏ hàng đã được đặt (bằng cách kiểm tra session hoặc database)
            if (Session["GioHang"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Lấy giỏ hàng từ session
            List<GioHang> lstGioHang = LayGioHang();

            // Kiểm tra xem giỏ hàng có sản phẩm hay không
            if (lstGioHang.Count == 0)
            {
                return RedirectToAction("Index", "Home");
            }

            // Lấy thông tin khách hàng từ session
            TaiKhoan kh = (TaiKhoan)Session["User"];

            // Lấy tổng số lượng và tổng tiền từ giỏ hàng
            ViewBag.TongSoLuong = TongSoLuong();
            ViewBag.TongTien = TongTien();

            // Trả về trang xác nhận đơn hàng với các thông tin giỏ hàng và khách hàng
            return View(lstGioHang);
        }

    }
}
