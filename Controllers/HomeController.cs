using System.Linq;
using System.Web.Mvc;
using APPLE_STORE.Models;
using Microsoft.AspNet.Identity;
using System.Net;
using System.Net.Mail;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Web;
using System.Globalization;
using System.Data.Entity.Validation;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Security;
using System.Data.Entity.Infrastructure;
using Org.BouncyCastle.Crypto.Generators;
using System.Data.SqlClient;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace APPLE_STORE.Controllers
{
    public class HomeController : Controller
    {
        private WebBanHangEntities2 db = new WebBanHangEntities2();

        public ActionResult Index()
        {
            // Lấy tất cả sản phẩm có trạng thái là hiển thị
            var allProducts = db.SanPhams
                                .Where(sp => sp.TrangThai == true) // Kiểm tra sản phẩm hiển thị
                                .ToList();

            // Lấy danh mục slider
            var sliders = db.DanhMucs.Where(dm => dm.TrangThai == true).Take(5).ToList();

            // Gửi dữ liệu sang view qua ViewBag
            ViewBag.AllProducts = allProducts;
            ViewBag.Sliders = sliders;

            return View();
        }

        public ActionResult PaymentPolicy()
        {
            return View(); // Đảm bảo bạn có một view PaymentPolicy.cshtml
        }

        public ActionResult TermsOfUse()
        {
            return View(); // Đảm bảo bạn có một view TermsOfUse.cshtml
        }

        // Giới thiệu ứng dụng
        public ActionResult About()
        {
            ViewBag.Message = "Đây là trang giới thiệu về ứng dụng của chúng tôi. Ứng dụng này được phát triển với mục đích mang lại trải nghiệm tuyệt vời cho người dùng.";
            return View();
        }

        public ActionResult Contact(string name, string email, string message)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string fromEmail = "2224801030216@student.tdmu.edu.vn"; // Địa chỉ email người gửi
                    string toEmail = "2224801030216@student.tdmu.edu.vn"; // Địa chỉ email người nhận
                    const string fromPassword = "rzvk uyur prtz wbyi"; // Mật khẩu ứng dụng Gmail
                    string subject = "Liên hệ từ: " + name;
                    string body = $"Tên: {name}\nEmail: {email}\nTin nhắn: {message}";

                    var smtp = new SmtpClient
                    {
                        Host = "smtp.gmail.com",
                        Port = 587, // Sử dụng cổng 587 cho TLS
                        EnableSsl = true, // Sử dụng SSL/TLS
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        Credentials = new NetworkCredential(fromEmail, fromPassword),
                        Timeout = 20000
                    };

                    smtp.Send(fromEmail, toEmail, subject, body); // Gửi email

                    ViewBag.Message = "Cảm ơn bạn! Tin nhắn của bạn đã được gửi đi.";
                }
                catch (Exception ex)
                {
                    ViewBag.Message = "Có lỗi xảy ra khi gửi tin nhắn. Vui lòng thử lại sau. Lỗi: " + ex.Message;
                    System.Diagnostics.Debug.WriteLine("Lỗi khi gửi email: " + ex.ToString()); // Log chi tiết lỗi
                }
            }

            return View();
        }


        [HttpPost]
        [ValidateInput(false)] // ⚡ Cho phép nhận HTML
        public ActionResult Search(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return View("ProductList", new List<SanPham>());
            }

            // ⚡ Encode keyword trước khi dùng để tránh XSS
            string safeKeyword = System.Web.HttpUtility.HtmlEncode(keyword);

            var result = db.SanPhams
                           .Where(p => p.Ten.Contains(safeKeyword) || p.MoTa.Contains(safeKeyword))
                           .ToList();

            return View("ProductList", result);
        }




        // Thêm một sản phẩm mới
        public ActionResult Product(int page = 1)
        {
            int pageSize = 5; // Số lượng sản phẩm mỗi trang
            int skip = (page - 1) * pageSize; // Vị trí bắt đầu lấy sản phẩm

            var productList = db.SanPhams
                                .OrderBy(sp => sp.Id) // Sắp xếp theo Id hoặc bất kỳ trường nào bạn muốn
                                .Skip(skip) // Bỏ qua sản phẩm đã hiển thị ở các trang trước
                                .Take(pageSize) // Lấy 5 sản phẩm
                                .ToList();

            // Tổng số sản phẩm để tính tổng số trang
            int totalProducts = db.SanPhams.Count();
            int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            // Truyền dữ liệu phân trang cho View
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(productList);
        }


        // Hiển thị các sản phẩm theo danh mục
        public ActionResult ProductList(int? categoryId, int page = 1)
        {
            if (categoryId == null)
            {
                return RedirectToAction("Index");
            }

            int pageSize = 5; // Số lượng sản phẩm mỗi trang
            int skip = (page - 1) * pageSize; // Vị trí bắt đầu lấy sản phẩm

            var products = db.SanPhams
                             .Where(sp => sp.IdDanhMuc == categoryId && sp.TrangThai == true)
                             .OrderBy(sp => sp.Id) // Sắp xếp theo Id hoặc bất kỳ trường nào bạn muốn
                             .Skip(skip) // Bỏ qua sản phẩm đã hiển thị ở các trang trước
                             .Take(pageSize) // Lấy 5 sản phẩm
                             .ToList();

            // Tổng số sản phẩm để tính tổng số trang
            int totalProducts = db.SanPhams.Count(sp => sp.IdDanhMuc == categoryId && sp.TrangThai == true);
            int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            // Truyền dữ liệu phân trang cho View
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.CategoryId = categoryId;

            return View(products);
        }


        // Chi tiết sản phẩm
        public ActionResult ProductDetails(int? id)
        {
            if (id == null)
            {
                return HttpNotFound();
            }

            var product = db.SanPhams.Find(id);
            if (product == null || product.TrangThai != true)
            {
                return HttpNotFound();
            }

            return View(product);
        }



        // Action cho trang đăng ký
        public ActionResult Register()
        {
            return View();
        }
        [HttpPost]
        [ValidateInput(false)] // ⚡ Cho phép nhận HTML
        public ActionResult Register(string tenTaiKhoan, string matKhau, string email, string hoTen, string diaChi, string soDienThoai)
        {
            // Kiểm tra các trường không được trống
            if (string.IsNullOrWhiteSpace(tenTaiKhoan) || string.IsNullOrWhiteSpace(matKhau) || string.IsNullOrWhiteSpace(email))
            {
                ViewBag.ErrorMessage = "Tên tài khoản, mật khẩu và email không được để trống.";
                return View();
            }

            // Encode input ngay lập tức chống XSS
            tenTaiKhoan = System.Web.HttpUtility.HtmlEncode(tenTaiKhoan);
            matKhau = System.Web.HttpUtility.HtmlEncode(matKhau);
            email = System.Web.HttpUtility.HtmlEncode(email);
            hoTen = System.Web.HttpUtility.HtmlEncode(hoTen);
            diaChi = System.Web.HttpUtility.HtmlEncode(diaChi);
            soDienThoai = System.Web.HttpUtility.HtmlEncode(soDienThoai);

            // Validate Tên tài khoản
            if (tenTaiKhoan.Length < 5 || tenTaiKhoan.Length > 20 || !Regex.IsMatch(tenTaiKhoan, @"^[a-zA-Z0-9]+$"))
            {
                ViewBag.ErrorMessage = "Tên tài khoản phải từ 5-20 ký tự và chỉ gồm chữ cái, số.";
                return View();
            }

            // Validate Mật khẩu (mạnh hơn)
            if (matKhau.Length < 8 ||
                !Regex.IsMatch(matKhau, @"[a-z]") ||
                !Regex.IsMatch(matKhau, @"[A-Z]") ||
                !Regex.IsMatch(matKhau, @"[0-9]") ||
                !Regex.IsMatch(matKhau, @"[\W_]"))
            {
                ViewBag.ErrorMessage = "Mật khẩu phải ít nhất 8 ký tự, có chữ thường, chữ hoa, số và ký tự đặc biệt.";
                return View();
            }

            // Validate Email
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ViewBag.ErrorMessage = "Email không hợp lệ.";
                return View();
            }

            // Validate Số điện thoại
            if (!string.IsNullOrEmpty(soDienThoai) && !Regex.IsMatch(soDienThoai, @"^\d{10}$"))
            {
                ViewBag.ErrorMessage = "Số điện thoại phải đúng 10 chữ số.";
                return View();
            }

            if (ModelState.IsValid)
            {
                // Kiểm tra tài khoản hoặc email đã tồn tại
                var existingAccount = db.TaiKhoans.FirstOrDefault(t => t.TenTaiKhoan == tenTaiKhoan || t.Email == email);
                if (existingAccount != null)
                {
                    ViewBag.ErrorMessage = "Tên tài khoản hoặc email đã tồn tại.";
                    return View();
                }

                // Lưu thông tin vào Session để xác thực OTP
                Session["Register_TenTaiKhoan"] = tenTaiKhoan;
                Session["Register_MatKhau"] = HashPassword(matKhau); // ⚡ Mã hóa mật khẩu SHA256
                Session["Register_Email"] = email;
                Session["Register_HoTen"] = hoTen;
                Session["Register_DiaChi"] = diaChi;
                Session["Register_SoDienThoai"] = soDienThoai;

                // Gửi OTP
                string otpCode = GenerateOTP();
                Session["OTP_Code"] = otpCode;
                Session["OTP_Email"] = email;
                Session["OTP_Time"] = DateTime.Now;

                SendConfirmationEmail(email, otpCode);

                return RedirectToAction("VerifyOTP");
            }

            return View();
        }


        private string GenerateOTP()
        {
            var random = new Random();
            int otp = random.Next(100000, 999999); // Sinh số ngẫu nhiên 6 chữ số
            return otp.ToString();
        }

        [HttpPost]
        public ActionResult ResendOTP()
        {
            var email = TempData["EmailForVerification"] as string;

            if (string.IsNullOrEmpty(email))
            {
                return new HttpStatusCodeResult(400, "Email không hợp lệ.");
            }

            var account = db.TaiKhoans.FirstOrDefault(t => t.Email == email);

            if (account == null)
            {
                return new HttpStatusCodeResult(404, "Không tìm thấy tài khoản.");
            }

            // Tạo OTP mới
            string newOtp = GenerateOTP();
            account.MaOTP = newOtp;
            account.OtpCreatedTime = DateTime.Now;
            db.SaveChanges();

            // Gửi OTP mới
            SendConfirmationEmail(email, newOtp);

            TempData["EmailForVerification"] = email; // Cần giữ lại TempData

            return new HttpStatusCodeResult(200);
        }


        // Hàm kiểm tra email hợp lệ
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        [HttpGet]
        public ActionResult VerifyOTP()
        {
            return View();
        }

        [HttpPost]
        [ValidateInput(false)] // ⚡

        public ActionResult VerifyOTPInline(string otp)
        {
            var email = Session["Register_Email"] as string;
            var otpCode = Session["OTP_Code"] as string;
            var otpEmail = Session["OTP_Email"] as string;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otp) || otpCode == null || otpEmail == null)
            {
                ViewBag.ErrorMessage = "Thông tin không hợp lệ.";
                return View("VerifyOTP");
            }

            if (email != otpEmail || otp != otpCode)
            {
                ViewBag.ErrorMessage = "Mã OTP không chính xác.";
                return View("VerifyOTP");
            }

            // Xác thực thành công, lưu vào database
            var taiKhoan = new TaiKhoan
            {
                TenTaiKhoan = Session["Register_TenTaiKhoan"].ToString(),
                MatKhau = Session["Register_MatKhau"].ToString(),
                Email = email,
                VaiTro = "KhachHang",
                HoTen = Session["Register_HoTen"].ToString(),
                DiaChi = Session["Register_DiaChi"].ToString(),
                SoDienThoai = Session["Register_SoDienThoai"].ToString(),
                XacThucEmail = true,
                TrangThai = true,
            };

            db.TaiKhoans.Add(taiKhoan);
            db.SaveChanges();

            Session.Clear();

            return RedirectToAction("Login", "Home");
        }




        // Phương thức tạo OTP mới và gửi mail xác nhận
        private void SendConfirmationEmail(string email, string otp)
        {
            var fromEmail = "2224801030216@student.tdmu.edu.vn"; // Email gửi đi
            var emailPassword = "rzvk uyur prtz wbyi"; // Mật khẩu ứng dụng
            var subject = "Mã xác thực OTP - APPLE STORE";

            var body = $@"
        <h2>Xác thực đăng ký tài khoản</h2>
        <p>Xin chào,</p>
        <p>Mã OTP của bạn là: <strong>{otp}</strong></p>
        <p>Mã có hiệu lực trong 5 phút.</p>
        <br>
        <p>Trân trọng,<br>APPLE STORE Team</p>
    ";

            using (var smtpClient = new SmtpClient("smtp.gmail.com"))
            {
                smtpClient.Port = 587;
                smtpClient.Credentials = new NetworkCredential(fromEmail, emailPassword);
                smtpClient.EnableSsl = true;

                using (var mailMessage = new MailMessage(fromEmail, email, subject, body))
                {
                    mailMessage.IsBodyHtml = true;
                    smtpClient.Send(mailMessage);
                }
            }
        }



        // Trang đăng nhập
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateInput(false)] // ⚡ Cho phép dữ liệu HTML
        public async Task<ActionResult> Login(TaiKhoan account)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.LoginError = "Vui lòng điền đầy đủ thông tin.";
                return View(account);
            }

            var recaptchaResponse = Request["g-recaptcha-response"];

            if (string.IsNullOrEmpty(recaptchaResponse))
            {
                ViewBag.LoginError = "Vui lòng xác nhận reCAPTCHA.";
                return View(account);
            }

            bool isCaptchaValid = await VerifyCaptcha(recaptchaResponse);
            if (!isCaptchaValid)
            {
                ViewBag.LoginError = "Xác thực reCAPTCHA không hợp lệ.";
                return View(account);
            }

            if (string.IsNullOrEmpty(account.TenTaiKhoan) || string.IsNullOrEmpty(account.MatKhau))
            {
                ViewBag.LoginError = "Tên tài khoản/Email và mật khẩu không được để trống.";
                return View(account);
            }

            string matKhauMaHoa = HashPassword(account.MatKhau);

            // ⚡ Tìm user theo tên tài khoản HOẶC email
            var user = db.TaiKhoans.FirstOrDefault(u =>
                (u.TenTaiKhoan == account.TenTaiKhoan || u.Email == account.TenTaiKhoan) &&
                u.MatKhau == matKhauMaHoa
            );

            if (user != null)
            {
                Session["User"] = user;
                Session["DienThoai"] = user.SoDienThoai;
                Session["DiaChi"] = user.DiaChi;
                Session["HoTen"] = user.HoTen;

                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewBag.LoginError = "Tên tài khoản/Email hoặc mật khẩu không đúng.";
                return View(account);
            }
        }


        //Mã hóa mật khẩu bằng SHA256
        public string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }
        private async Task<bool> VerifyCaptcha(string recaptchaResponse)
        {
            using (var client = new HttpClient())
            {
                var values = new Dictionary<string, string>
            {
                { "secret", "6LeNFiYrAAAAAMX5uboFa8PVt-k-LMu6ixLio58F" }, // Thay bằng Secret Key của bạn
                { "response", recaptchaResponse }
            };

                var content = new FormUrlEncodedContent(values);
                var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
                var responseString = await response.Content.ReadAsStringAsync();

                var captchaResult = JsonConvert.DeserializeObject<ReCaptchaResponse>(responseString);

                return captchaResult.Success;
            }
        }

        public class ReCaptchaResponse
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("error-codes")]
            public string[] ErrorCodes { get; set; }
       }

        // Xử lý đăng xuất
        public ActionResult Logout()
        {
            Session.Clear();  // Xóa toàn bộ Session
            TempData["Message"] = "Bạn đã đăng xuất!";
            return RedirectToAction("Index", "Home");
        }

        // Thêm vào trong HomeController
        private bool IsAdmin()
        {
            var user = Session["User"] as TaiKhoan;
            return user != null && user.VaiTro == "QuanTri";
        }

        // Trang quản lý đơn hàng cho Admin
        public ActionResult ManageOrders()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Error403", "Home");
            }

            var orders = db.DonHangs.ToList();
            return View(orders);
        }

        // Action để chỉnh sửa đơn hàng
        public ActionResult EditOrder(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Error403", "Home");
            }

            var order = db.DonHangs.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }
            return View(order);
        }

        // Xử lý chỉnh sửa đơn hàng
        [HttpPost]
        public ActionResult EditOrder(DonHang order)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Error403", "Home");
            }

            if (ModelState.IsValid)
            {
                db.Entry(order).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("ManageOrders");
            }
            return View(order);
        }

        // Action để xóa đơn hàng
        public ActionResult DeleteOrder(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Error403", "Home");
            }

            var order = db.DonHangs.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }

            db.DonHangs.Remove(order);
            db.SaveChanges();
            return RedirectToAction("ManageOrders");
        }

        // Trang quản lý sản phẩm cho Admin
        public ActionResult ManageProducts()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Error403", "Home");
            }

            var products = db.SanPhams.ToList();
            return View(products);
        }

        // Action để chỉnh sửa sản phẩm
        public ActionResult EditProduct(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Error403", "Home");
            }

            var product = db.SanPhams.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // Xử lý chỉnh sửa sản phẩm
        [HttpPost]
        public ActionResult EditProduct(SanPham product)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Error403", "Home");
            }

            if (ModelState.IsValid)
            {
                db.Entry(product).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("ManageProducts");
            }
            return View(product);
        }

        // Action để xóa sản phẩm
        public ActionResult DeleteProduct(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Error403", "Home");
            }

            var product = db.SanPhams.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }

            var chiTietDonHangs = db.ChiTietDonHangs.Where(ct => ct.IdSanPham == id).ToList();
            foreach (var chiTiet in chiTietDonHangs)
            {
                db.ChiTietDonHangs.Remove(chiTiet);
            }

            var gioHangs = db.GioHangs.Where(gh => gh.MaSanPham == id).ToList();
            foreach (var gioHang in gioHangs)
            {
                db.GioHangs.Remove(gioHang);
            }

            var danhGias = db.DanhGias.Where(dg => dg.IdSanPham == id).ToList();
            foreach (var danhGia in danhGias)
            {
                db.DanhGias.Remove(danhGia);
            }

            db.SanPhams.Remove(product);
            db.SaveChanges();
            return RedirectToAction("ManageProducts");
        }

        // Trang quản lý người dùng cho Admin
        public ActionResult ManageUsers()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Error403", "Home");
            }

            var users = db.TaiKhoans.ToList();
            return View(users);
        }

        // Action để chỉnh sửa người dùng
        public ActionResult EditUser(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Error403", "Home");
            }

            var user = db.TaiKhoans.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        [HttpPost]
        public ActionResult EditUser(TaiKhoan updatedUser)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Error403", "Home");
            }

            if (ModelState.IsValid)
            {
                // ⚡ Load bản ghi gốc từ DB theo IdTaiKhoan
                var user = db.TaiKhoans.Find(updatedUser.IdTaiKhoan);

                if (user == null)
                {
                    return HttpNotFound();
                }

                // ⚡ Cập nhật lại các trường
                user.TenTaiKhoan = updatedUser.TenTaiKhoan;
                user.MatKhau = updatedUser.MatKhau;
                user.Email = updatedUser.Email;
                user.HoTen = updatedUser.HoTen;
                user.DiaChi = updatedUser.DiaChi;
                user.SoDienThoai = updatedUser.SoDienThoai;
                user.VaiTro = updatedUser.VaiTro;
                user.XacThucEmail = updatedUser.XacThucEmail;
                user.TrangThai = updatedUser.TrangThai;

                // ⚡ Bây giờ mới Save
                db.SaveChanges();

                return RedirectToAction("ManageUsers");
            }

            return View(updatedUser);
        }


        // Action để xóa người dùng
        public ActionResult DeleteUser(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Error403", "Home");
            }

            var user = db.TaiKhoans.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }

            var donHangs = db.DonHangs.Where(dh => dh.IdNguoiDung == id).ToList();
            foreach (var donHang in donHangs)
            {
                var chiTietDonHangs = db.ChiTietDonHangs.Where(ct => ct.IdDonHang == donHang.Id).ToList();
                foreach (var chiTiet in chiTietDonHangs)
                {
                    db.ChiTietDonHangs.Remove(chiTiet);
                }
                db.DonHangs.Remove(donHang);
            }

            var gioHangs = db.GioHangs.Where(gh => gh.MaNguoiDung == id).ToList();
            foreach (var gioHang in gioHangs)
            {
                db.GioHangs.Remove(gioHang);
            }

            var danhGias = db.DanhGias.Where(dg => dg.IdNguoiDung == id).ToList();
            foreach (var danhGia in danhGias)
            {
                db.DanhGias.Remove(danhGia);
            }

            db.TaiKhoans.Remove(user);
            db.SaveChanges();
            return RedirectToAction("ManageUsers");
        }

        // Tạo sản phẩm mới cho Admin
        public ActionResult CreateProduct()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Error403", "Home");
            }

            ViewBag.DanhMucList = db.DanhMucs.ToList();
            return View();
        }
        public ActionResult Error403()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateProduct(SanPham model, HttpPostedFileBase HinhAnh)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Error403", "Home");
            }

            if (ModelState.IsValid)
            {
                model.ConHang = true;
                model.TrangThai = true;
                model.NgayTao = DateTime.Now;
                model.NgayCapNhat = DateTime.Now;

                if (HinhAnh != null && HinhAnh.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(HinhAnh.FileName);
                    string path = Path.Combine(Server.MapPath("~/Images/"), fileName);
                    HinhAnh.SaveAs(path);
                    model.HinhAnh = fileName;
                }

                db.SanPhams.Add(model);
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.DanhMucList = db.DanhMucs.ToList();
            return View(model);
        }


    }
}
