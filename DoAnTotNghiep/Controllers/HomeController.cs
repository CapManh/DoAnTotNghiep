using BCrypt.Net;
using DoAnTotNghiep.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace DoAnTotNghiep.Controllers
{
    
    public class HomeController : Controller
    {
        private Model1 db = new Model1();
        [HttpPost]
        public JsonResult Login(string Ten, string MatKhau)
        {
            var user = db.NguoiDungs.FirstOrDefault(x => x.Ten == Ten);

            if (user != null)
            {
                bool isPasswordCorrect = false;
                if (user.IsActive == false)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Tài khoản của bạn hiện tại đã bị khóa. Vui lòng liên hệ quản trị viên để được hỗ trợ!"
                    });
                }

                try
                {
                    isPasswordCorrect = BCrypt.Net.BCrypt.Verify(MatKhau, user.MatKhau);
                }
                catch
                {
                    isPasswordCorrect = (user.MatKhau == MatKhau);
                }

                if (isPasswordCorrect)
                {
                    Session["MaNguoiDung"] = user.MaNguoiDung;
                    Session["TenNguoiDung"] = user.Ten;
                    Session["VaiTro"] = user.MaVaiTro;
                    Session["Email"] = user.Email;
                    Session["SDT"] = user.SoDienThoai;
                    Session["DiaChi"] = user.DiaChi;

                    string redirectUrl = "/";

                    switch (user.MaVaiTro)
                    {
                        case 1:
                            redirectUrl = Url.Action("ThongKe", "Admin");
                            break;

                        case 2:
                            redirectUrl = Url.Action("TrangChu", "Home");
                            break;

                        case 3:
                            redirectUrl = Url.Action("ThongKe", "Admin");
                            break;

                        case 5:
                            redirectUrl = Url.Action("ThongKe", "Admin");
                            break;
                    }

                    return Json(new
                    {
                        success = true,
                        message = "Đăng nhập thành công!",
                        redirect = redirectUrl
                    });
                }
            }

            return Json(new { success = false, message = "Sai tài khoản hoặc mật khẩu!" });
        }
        public ActionResult LoginGoogle()
        {
            HttpContext.GetOwinContext().Authentication.Challenge(
                new Microsoft.Owin.Security.AuthenticationProperties
                {
                    RedirectUri = "/Home/GoogleCallback"
                },
                "Google"
            );

            return new HttpUnauthorizedResult();
        }
        public ActionResult GoogleCallback()
        {
            var identity = (System.Security.Claims.ClaimsIdentity)User.Identity;

            var email = identity.Claims.FirstOrDefault(x => x.Type == "Email")?.Value;
            var name = identity.Claims.FirstOrDefault(x => x.Type == "Name")?.Value;

            if (email == null)
            {
                return RedirectToAction("Login", "Home");
            }

            var user = db.NguoiDungs.FirstOrDefault(x => x.Email == email);

            if (user == null)
            {
                user = new NguoiDung
                {
                    Email = email,
                    Ten = name,
                    IsActive = true,
                    MatKhau = "",
                    MaVaiTro = 2,
                    NgayTao = DateTime.Now
                };

                db.NguoiDungs.Add(user);
                db.SaveChanges();
            }

            // 👉 dùng lại session của bạn
            Session["MaNguoiDung"] = user.MaNguoiDung;
            Session["TenNguoiDung"] = user.Ten;
            Session["VaiTro"] = user.MaVaiTro;
            Session["Email"] = user.Email;

            return RedirectToAction("TrangChu", "Home");
        }
        // GET: Quên mật khẩu
        public ActionResult QuenMatKhau()
        {
            return View();
        }

        // POST: Quên mật khẩu
        [HttpPost]
        public ActionResult QuenMatKhau(string email)
        {
            var user = db.NguoiDungs.FirstOrDefault(x => x.Email == email);

            if (user == null)
            {
                ViewBag.Error = "Email không tồn tại!";
                return View();
            }

            // tạo OTP
            Random rd = new Random();
            string otp = rd.Next(100000, 999999).ToString();

            user.OTP = otp;
            user.OTP_HetHan = DateTime.Now.AddMinutes(5);
            db.SaveChanges();

            // gửi mail
            MailHelper.SendOTP(email, otp);

            TempData["Email"] = email;

            return RedirectToAction("XacNhanOTP");
        }
        public ActionResult XacNhanOTP()
        {
            return View();
        }

        [HttpPost]
        public ActionResult XacNhanOTP(string otp)
        {
            string email = TempData["Email"]?.ToString();

            var user = db.NguoiDungs.FirstOrDefault(x => x.Email == email);

            if (user == null)
            {
                return RedirectToAction("QuenMatKhau");
            }

            if (user.OTP != otp)
            {
                ViewBag.Error = "Sai OTP!";
                return View();
            }

            if (user.OTP_HetHan < DateTime.Now)
            {
                ViewBag.Error = "OTP đã hết hạn!";
                return View();
            }

            TempData["ResetEmail"] = email;
            return RedirectToAction("DoiMatKhau");
        }

        public static void SendOTP(string toEmail, string otp)
        {
            var fromEmail = "buimanhtuan009@gmail.com";
            var appPassword = "folv renl feph vfew"; // thay bằng app password của bạn

            var message = new MailMessage();
            message.From = new MailAddress(fromEmail, "Nội Thất Shop");
            message.To.Add(toEmail);
            message.Subject = "Mã OTP khôi phục mật khẩu";
            message.Body = $"Mã OTP của bạn là: <b>{otp}</b>";
            message.IsBodyHtml = true;

            var smtp = new SmtpClient("smtp.gmail.com", 587);
            smtp.Credentials = new NetworkCredential(fromEmail, appPassword);
            smtp.EnableSsl = true;

            smtp.Send(message);
        }
        public ActionResult DoiMatKhau()
        {
            return View();
        }

        [HttpPost]
        public ActionResult DoiMatKhau(string matKhauMoi)
        {
            string email = TempData["ResetEmail"]?.ToString();

            var user = db.NguoiDungs.FirstOrDefault(x => x.Email == email);

            if (user == null)
            {
                return RedirectToAction("QuenMatKhau");
            }

            user.MatKhau = BCrypt.Net.BCrypt.HashPassword(matKhauMoi);

            // xóa OTP sau khi dùng
            user.OTP = null;
            user.OTP_HetHan = null;

            db.SaveChanges();

            ViewBag.Success = "Đổi mật khẩu thành công!";
            return View();
        }
        public ActionResult SanPhamTheoDanhMuc(int id)
        {
            var danhMuc = db.DanhMuc.Find(id);

            ViewBag.TenDanhMucDangChon = danhMuc?.TenDanhMuc;

            var sanPhams = db.SanPham
                             .Where(x => x.MaDanhMuc == id)
                             .ToList();

            return View(sanPhams);
        }
        [HttpPost]
        public JsonResult ThemGioHang(int id, int quantity = 1)
        {
            try
            {
                if (Session["MaNguoiDung"] == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Vui lòng đăng nhập!"
                    });
                }

                if (quantity < 1) quantity = 1;

                int maND = Convert.ToInt32(Session["MaNguoiDung"]);

                int tonKho = db.ChiTietSanPham
                    .Where(x => x.MaSanPham == id)
                    .Select(x => x.SoLuongTon ?? 0)
                    .FirstOrDefault();

                if (tonKho <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Sản phẩm đã hết hàng"
                    });
                }

                var item = db.GioHang
                    .FirstOrDefault(x =>
                        x.MaNguoiDung == maND &&
                        x.MaSanPham == id);

                if (item == null)
                {
                    if (quantity > tonKho)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Số lượng đã hết vui lòng chọn sản phẩm khác"
                        });
                    }

                    db.GioHang.Add(new GioHang
                    {
                        MaNguoiDung = maND,
                        MaSanPham = id,
                        SoLuong = quantity,
                        NgayTao = DateTime.Now
                    });
                }
                else
                {
                    int tongSoLuong = (item.SoLuong ?? 0) + quantity;

                    if (tongSoLuong > tonKho)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Số lượng đã hết vui lòng chọn sản phẩm khác"
                        });
                    }

                    item.SoLuong = tongSoLuong;
                }

                db.SaveChanges();

                return LayMiniCart(maND);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        private JsonResult LayMiniCart(int maND)
        {
            db.Configuration.ProxyCreationEnabled = false;

            var cart = db.GioHang
                .Include(x => x.SanPham)
                .Where(x => x.MaNguoiDung == maND)
                .ToList();

            return Json(new
            {
                success = true,
                totalQuantity = cart.Sum(x => x.SoLuong),
                totalAmount = cart.Sum(x => x.SoLuong * (x.SanPham.Gia ?? 0)),
                cartItems = cart.Select(x => new
                {
                    MaSP = x.MaSanPham,
                    TenSP = x.SanPham.TenSanPham,
                    AnhChinh = x.SanPham.AnhChinh,

                    Gia = (x.SanPham.KhuyenMai != null &&
                       x.SanPham.KhuyenMai.PhanTramGiam > 0)
                    ? (x.SanPham.Gia ?? 0)
                      - ((x.SanPham.Gia ?? 0)
                      * x.SanPham.KhuyenMai.PhanTramGiam / 100)
                    : (x.SanPham.Gia ?? 0),

                    GiaGoc = x.SanPham.Gia ?? 0,

                    SL = x.SoLuong,

                    TonKho = db.ChiTietSanPham
                    .Where(ct => ct.MaSanPham == x.MaSanPham)
                    .Select(ct => ct.SoLuongTon)
                    .FirstOrDefault(),

                    Link = "/Home/chitietsanpham/" + x.MaSanPham
                })
            });
        }
        [HttpGet]
        public JsonResult LayGioHang()
        {
            if (Session["MaNguoiDung"] == null)
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            int maND = Convert.ToInt32(Session["MaNguoiDung"]);

            db.Configuration.ProxyCreationEnabled = false;

            var cart = db.GioHang
                .Include(x => x.SanPham)
                .Where(x => x.MaNguoiDung == maND)
                .AsEnumerable()
                .Select(x => new
                {
                    MaSP = x.MaSanPham,
                    TenSP = x.SanPham.TenSanPham,
                    AnhChinh = x.SanPham.AnhChinh,
                    Gia = x.SanPham.Gia ?? 0,
                    SL = x.SoLuong ?? 0,
                    Link = "/Home/chitietsanpham/" + x.MaSanPham,
                    ThanhTien = (x.SanPham.Gia ?? 0) * (x.SoLuong ?? 0)
                })
                .ToList();

            return Json(new
            {
                success = true,
                totalQuantity = cart.Sum(x => x.SL),
                totalAmount = cart.Sum(x => x.ThanhTien),
                cartItems = cart
            }, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult UpdateProfile(string Ten, string Email, string SoDienThoai, string DiaChi)
        {
            try
            {
                if (Session["MaNguoiDung"] == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập lại!" });
                }

                int maND = int.Parse(Session["MaNguoiDung"].ToString());
                var user = db.NguoiDung.SingleOrDefault(x => x.MaNguoiDung == maND);

                if (user != null)
                {
                    user.Ten = Ten;
                    user.Email = Email;
                    user.SoDienThoai = SoDienThoai;
                    user.DiaChi = DiaChi;
                    db.SaveChanges();

                    Session["TenNguoiDung"] = Ten;
                    Session["Email"] = Email;
                    Session["SDT"] = SoDienThoai;
                    Session["DiaChi"] = DiaChi;

                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Không tìm thấy người dùng!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
        [HttpPost]
        public JsonResult XoaGioHang(int id)
        {
            if (Session["MaNguoiDung"] == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập lại!" });
            }

            int maND = Convert.ToInt32(Session["MaNguoiDung"]);

            var item = db.GioHang
                .FirstOrDefault(x => x.MaNguoiDung == maND && x.MaSanPham == id);

            if (item != null)
            {
                db.GioHang.Remove(item);
                db.SaveChanges();
            }
            else
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng!" });
            }

            var cart = db.GioHang
                .Include(x => x.SanPham)
                .Where(x => x.MaNguoiDung == maND)
                .Select(x => new
                {
                    MaSP = x.MaSanPham,
                    TenSP = x.SanPham.TenSanPham,
                    AnhChinh =x.SanPham.AnhChinh,
                    Gia = x.SanPham.Gia ?? 0,
                    SL = x.SoLuong ?? 0,
                    ThanhTien = (x.SanPham.Gia ?? 0) * (x.SoLuong ?? 0)
                })
                .ToList();

            return Json(new
            {
                success = true,
                message = "Đã xóa sản phẩm khỏi giỏ hàng!",
                totalQuantity = cart.Sum(x => x.SL),
                totalAmount = cart.Sum(x => x.ThanhTien),
                cartItems = cart
            });
        }
        [HttpPost]
        public JsonResult UpdatePassword(string OldPass, string NewPass)
        {
            try
            {
                if (Session["MaNguoiDung"] == null)
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });

                int maND = int.Parse(Session["MaNguoiDung"].ToString());
                var user = db.NguoiDung.Find(maND);

                if (user != null)
                {
                    bool isOldPassCorrect = false;

                    try
                    {
                        isOldPassCorrect = BCrypt.Net.BCrypt.Verify(OldPass, user.MatKhau);
                    }
                    catch
                    {
                        isOldPassCorrect = (user.MatKhau == OldPass);
                    }

                    if (!isOldPassCorrect)
                    {
                        return Json(new { success = false, message = "Mật khẩu hiện tại không đúng!" });
                    }
                    user.MatKhau = BCrypt.Net.BCrypt.HashPassword(NewPass);
                    db.SaveChanges();

                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Lỗi xác thực!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
        [HttpPost]
        public JsonResult Register(NguoiDung model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
            }

            if (string.IsNullOrWhiteSpace(model.Ten) || model.Ten.Trim().Length < 5)
            {
                return Json(new
                {
                    success = false,
                    message = "Tên phải có ít nhất 5 ký tự!"
                });
            }

            if (string.IsNullOrWhiteSpace(model.Email) ||
                !Regex.IsMatch(model.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                return Json(new
                {
                    success = false,
                    message = "Email không hợp lệ!"
                });
            }

            if (string.IsNullOrWhiteSpace(model.SoDienThoai) ||
                !Regex.IsMatch(model.SoDienThoai, @"^0\d{9}$"))
            {
                return Json(new
                {
                    success = false,
                    message = "Số điện thoại phải 10 số và bắt đầu bằng 0!"
                });
            }
            if (string.IsNullOrWhiteSpace(model.MatKhau) || model.MatKhau.Length < 6)
            {
                return Json(new
                {
                    success = false,
                    message = "Mật khẩu phải ít nhất 6 ký tự!"
                });
            }

            try
            {
                var checkEmail = db.NguoiDung.FirstOrDefault(x => x.Email == model.Email);
                if (checkEmail != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Email này đã được sử dụng!"
                    });
                }

                var checkSDT = db.NguoiDung.FirstOrDefault(x => x.SoDienThoai == model.SoDienThoai);
                if (checkSDT != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Số điện thoại đã được sử dụng!"
                    });
                }

                model.NgayTao = DateTime.Now;
                model.MaVaiTro = 2;
                model.IsActive = true;

                db.NguoiDung.Add(model);
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Tạo tài khoản thành công."
                });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

                return Json(new
                {
                    success = false,
                    message = "Lỗi hệ thống: " + msg
                });
            }
        }
    public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("TrangChu", "Home");
        }
        private void LoadMenu()
        {
            ViewBag.DanhMucMenu = db.DanhMuc.ToList();
        }
        public ActionResult TrangChu()
        {
            var banners = db.Banner
                .Where(b => b.TrangThai == true
                         && b.NgayBatDau <= DateTime.Now
                         && b.NgayKetThuc >= DateTime.Now)
                .OrderBy(b => b.ThuTu)
                .ToList();
            ViewBag.BannerTopLeft = banners.FirstOrDefault(b => b.ThuTu == 1);
            ViewBag.BannerTopRight = banners.FirstOrDefault(b => b.ThuTu == 2);
            ViewBag.BannerBottomLeft = banners.FirstOrDefault(b => b.ThuTu == 3);
            ViewBag.BannerBottomRight = banners.FirstOrDefault(b => b.ThuTu == 4);
            ViewBag.BannerBottomThird = banners.FirstOrDefault(b => b.ThuTu == 5);
            DateTime ngay30NgayTruoc = DateTime.Now.AddDays(-30);
            var sanPhamMoi = db.SanPham
     .Where(sp => sp.NoiBat == true || sp.NgayTao >= ngay30NgayTruoc)
     .OrderByDescending(sp => sp.NgayTao)
     .Take(8)
     .ToList();
            ViewBag.SaoSanPham = db.DanhGia
                .GroupBy(dg => dg.MaSanPham)
                .ToDictionary(
                    g => g.Key,
                    g => g.Average(x => x.SoSao)
                );

            ViewBag.SanPhamMoi = sanPhamMoi;
            var sanPhamThinhHanh = db.SanPham
                                     .OrderByDescending(sp => sp.DanhGia.Count())
                                     .Take(8)
                                     .ToList();
            ViewBag.SaoSanPham = db.DanhGia
               .GroupBy(dg => dg.MaSanPham)
               .ToDictionary(
                   g => g.Key,
                   g => g.Average(x => x.SoSao)
               );

            ViewBag.SanPhamMoi = sanPhamMoi;
            ViewBag.SanPhamThinhHanh = sanPhamThinhHanh;
            LoadMenu();
            return View(banners);
        }
        public ActionResult GioiThieu()
        {
            LoadMenu();
            return View();
        }
        public ActionResult sanpham1(
          string sort = "mac-dinh",
          int? danhMuc = null,
          int[] thuongHieu = null,
          int[] mauSac = null,
          decimal? giaDen = null,
          int? page = 1,
          string keyword = null)
        {
            int pageSize = 6;
            int pageNumber = (page ?? 1);
            var query = db.SanPham
                          .Include("ChiTietSanPham")
                          .AsQueryable();

            if (danhMuc.HasValue && danhMuc.Value > 0)
            {
                query = query.Where(sp => sp.MaDanhMuc == danhMuc.Value);
            }
            if (thuongHieu != null && thuongHieu.Length > 0)
            {
                var thuongHieuList = thuongHieu.ToList();
                query = query.Where(sp => sp.ChiTietSanPham
                    .Any(ct => ct.MaThuongHieu.HasValue && thuongHieuList.Contains(ct.MaThuongHieu.Value)));
            }
            if (mauSac != null && mauSac.Length > 0)
            {
                query = query.Where(sp => sp.ChiTietSanPham
                    .Any(ct => ct.MaMau.HasValue && mauSac.Contains(ct.MaMau.Value)));
            }
            if (giaDen.HasValue && giaDen.Value > 0)
            {
                query = query.Where(sp => sp.Gia <= giaDen.Value);
            }
            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(sp => sp.TenSanPham.ToLower().Contains(keyword));
            }
            switch (sort.ToLower())
            {
                case "gia-asc":
                    query = query.OrderBy(sp => sp.Gia);
                    ViewBag.SortOrder = "gia-asc";
                    break;

                case "gia-desc":
                    query = query.OrderByDescending(sp => sp.Gia);
                    ViewBag.SortOrder = "gia-desc";
                    break;

                default:
                    query = query.OrderByDescending(sp => sp.NoiBat)
                                 .ThenByDescending(sp => sp.NgayTao);
                    ViewBag.SortOrder = "mac-dinh";
                    break;
            }

            int totalProducts = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            var sanPhamList = query
                              .Skip((pageNumber - 1) * pageSize)
                              .Take(pageSize)
                              .ToList();
            ViewBag.SaoSanPham = db.DanhGia
                .GroupBy(dg => dg.MaSanPham)
                .ToDictionary(g => g.Key, g => g.Average(x => x.SoSao));
            ViewBag.SanPhamMoi = sanPhamList;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalProducts;
            ViewBag.PageSize = pageSize;

            ViewBag.DanhMucList = db.DanhMuc.ToList();
            ViewBag.ThuongHieuList = db.ThuongHieu.ToList();
            ViewBag.MauSacList = db.MauSac.ToList();

            ViewBag.SelectedDanhMuc = danhMuc;
            ViewBag.SelectedThuongHieu = thuongHieu ?? new int[0];
            ViewBag.SelectedMauSac = mauSac ?? new int[0];
            ViewBag.GiaDen = giaDen ?? 15000000m;

            ViewBag.SanPhamMoi = sanPhamList;
            LoadMenu();
            return View();
        }
        public ActionResult CSTT()
        {
            LoadMenu();
            return View();
        }
        public ActionResult CSQRT()
        {
            LoadMenu();
            return View();
        }
        public ActionResult CSMH()
        {
            LoadMenu();
            return View();
        }
        public ActionResult CSGH()
        {
            LoadMenu();
            return View();
        }
        public ActionResult CSDT()
        {
            LoadMenu();
            return View();
        }
        public ActionResult LienHe()
        {
            return View();
        }

        [HttpPost]
        public ActionResult LienHe(LienHe lh)
        {
            lh.NgayGui = DateTime.Now;
            lh.TrangThai = "Chưa xử lý";

            db.LienHes.Add(lh);
            db.SaveChanges();

            return RedirectToAction("LienHe");
        }
        public ActionResult TinTuc()
        {
            LoadMenu();
            return View();
        }
        public ActionResult TinTuc()
        {
            LoadMenu();
            return View();
        }
        public ActionResult GioHang()
        {
            if (Session["MaNguoiDung"] == null)
                return RedirectToAction("TrangChu", "Home");

            int maND = Convert.ToInt32(Session["MaNguoiDung"]);

            var cartItems = db.GioHang
                .Where(x => x.MaNguoiDung == maND)
                .Include(x => x.SanPham)
                .Include(x => x.SanPham.ChiTietSanPham)
                .OrderBy(x => x.NgayTao)
                .ToList();

            LoadMenu();
            return View(cartItems);
        }
        [HttpPost]
        public JsonResult CapNhatSoLuongGioHang(int maSanPham, int soLuong)
        {
            if (Session["MaNguoiDung"] == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            int maND = Convert.ToInt32(Session["MaNguoiDung"]);

            if (soLuong < 1) soLuong = 1;

            var item = db.GioHang
                .FirstOrDefault(x => x.MaNguoiDung == maND && x.MaSanPham == maSanPham);

            if (item == null)
                return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });

            var tonKho = db.ChiTietSanPham
                .Where(x => x.MaSanPham == maSanPham)
                .Select(x => x.SoLuongTon)
                .FirstOrDefault() ?? 0;

            if (soLuong > tonKho)
            {
                return Json(new { success = false, message = $"Chỉ còn {tonKho} sản phẩm trong kho!" });
            }

            item.SoLuong = soLuong;
            db.SaveChanges();

            return Json(new { success = true });
        }
        public ActionResult chitietsanpham(int id)
        {
            var sanPham = db.SanPhams
                .FirstOrDefault(sp => sp.MaSanPham == id);

            if (sanPham == null)
            {
                return HttpNotFound();
            }

            var chiTiet = db.ChiTietSanPhams
                .Include("ThuongHieu")
                .Include("ChatLieu")
                .Include("MauSac")
                .FirstOrDefault(ct => ct.MaSanPham == id);
            int soLuongTon = 0;
            if (chiTiet != null)
            {
                soLuongTon = chiTiet.SoLuongTon ?? 0;
            }
            else
            {
                soLuongTon = 0;
            }

            int reviewCount = 0;
            double averageRating = 0.0;
            try
            {
                reviewCount = db.DanhGias.Count(r => r.MaSanPham == id);
                if (reviewCount > 0)
                {
                    averageRating = db.DanhGias
                        .Where(r => r.MaSanPham == id)
                        .Average(r => (double)r.SoSao);
                }
            }
            catch { }

            int soldCount = 0;
            try
            {
                var total = db.ChiTietDonHangs
                    .Join(db.ChiTietSanPhams,
                        cd => cd.MaChiTietSanPham,
                        ct => ct.MaChiTiet,
                        (cd, ct) => new { cd, ct })
                    .Where(x => x.ct.MaSanPham == id)
                    .Sum(x => (int?)x.cd.SoLuong);
                soldCount = total ?? 0;
            }
            catch { }

            var danhGias = db.DanhGias
                .Include(x => x.NguoiDung)
                .Where(x => x.MaSanPham == id)
                .OrderByDescending(x => x.NgayDanhGia)
                .ToList();
            int maND = 0;
            bool duocDanhGia = false;

            if (Session["MaNguoiDung"] != null)
            {
                maND = Convert.ToInt32(Session["MaNguoiDung"]);

                duocDanhGia = db.ChiTietDonHangs
                    .Join(db.DonHangs,
                        ct => ct.MaDonHang,
                        dh => dh.MaDonHang,
                        (ct, dh) => new { ct, dh })
                    .Join(db.ChiTietSanPhams,
                        temp => temp.ct.MaChiTietSanPham,
                        spct => spct.MaChiTiet,
                        (temp, spct) => new { temp.dh, spct })
                    .Any(x =>
                        x.dh.MaNguoiDung == maND &&
                        x.spct.MaSanPham == id &&
                        (x.dh.TrangThai == "Hoàn thành" || x.dh.TrangThai == "Đã giao hàng")
                    );
            }

            ViewBag.DuocDanhGia = duocDanhGia;

            ViewBag.DanhGias = danhGias;
            ViewBag.ChiTietSanPham = chiTiet;
            ViewBag.AverageRating = averageRating;
            ViewBag.ReviewCount = reviewCount;
            ViewBag.SoldCount = soldCount;
            ViewBag.SoLuongTon = soLuongTon;
            ViewBag.DanhGias = danhGias;

            LoadMenu();
            return View(sanPham);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult GuiDanhGia(int MaSanPham, int SoSao, string NoiDung)
        {
            if (Session["MaNguoiDung"] == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            if (SoSao <= 0)
                return Json(new { success = false, message = "Vui lòng chọn sao!" });

            int maND = Convert.ToInt32(Session["MaNguoiDung"]);

            // 🔥 CHECK ĐÃ MUA VÀ HOÀN THÀNH CHƯA
            bool daMua = db.ChiTietDonHangs
                .Join(db.DonHangs,
                    ct => ct.MaDonHang,
                    dh => dh.MaDonHang,
                    (ct, dh) => new { ct, dh })
                .Join(db.ChiTietSanPhams,
                    temp => temp.ct.MaChiTietSanPham,
                    spct => spct.MaChiTiet,
                    (temp, spct) => new { temp.dh, spct })
                .Any(x =>
                    x.dh.MaNguoiDung == maND &&
                    x.spct.MaSanPham == MaSanPham &&
                    (x.dh.TrangThai == "Hoàn thành" || x.dh.TrangThai == "Đã giao hàng")
                );

            if (!daMua)
            {
                return Json(new
                {
                    success = false,
                    message = "Bạn chỉ có thể đánh giá sau khi nhận hàng!"
                });
            }

            // 🔥 CHECK ĐÃ ĐÁNH GIÁ CHƯA (tránh spam)
            bool daDanhGia = db.DanhGias.Any(x =>
                x.MaNguoiDung == maND && x.MaSanPham == MaSanPham);

            if (daDanhGia)
            {
                return Json(new
                {
                    success = false,
                    message = "Bạn đã đánh giá sản phẩm này rồi!"
                });
            }

            // ✅ OK thì mới cho lưu
            var dg = new DanhGia()
            {
                MaNguoiDung = maND,
                MaSanPham = MaSanPham,
                SoSao = SoSao,
                NoiDung = NoiDung,
                NgayDanhGia = DateTime.Now
            };

            db.DanhGias.Add(dg);
            db.SaveChanges();

            var user = db.NguoiDungs.Find(maND);

            var danhGias = db.DanhGias
                .Where(x => x.MaSanPham == MaSanPham)
                .ToList();

            double avgStar = danhGias.Average(x => x.SoSao ?? 0);
            int reviewCount = danhGias.Count;

            return Json(new
            {
                success = true,
                tenNguoiDung = user.Ten,
                soSao = SoSao,
                noiDung = NoiDung,
                ngay = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                avgStar,
                reviewCount
            });
        }
        public JsonResult Suggest(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);

            keyword = keyword.Trim().ToLower();

            var data = db.SanPham
                .AsEnumerable()
                .Where(p => !string.IsNullOrEmpty(p.TenSanPham) &&
                            p.TenSanPham.ToLower().Contains(keyword))
                .Take(3)
                .Select(p => new
                {
                    p.MaSanPham,
                    p.TenSanPham,
                    AnhChinh = p.AnhChinh
                })
                .ToList();

            return Json(data, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult ApDungVoucher(string code, decimal tongTien)
        {
            if (Session["MaNguoiDung"] == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Vui lòng đăng nhập"
                });
            }

            int maND = Convert.ToInt32(Session["MaNguoiDung"]);

            var voucher = db.KhuyenMai
                .FirstOrDefault(x => x.MaCode == code);

            if (voucher == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Mã không tồn tại"
                });
            }

            if (voucher.NgayBatDau > DateTime.Now ||
                voucher.NgayKetThuc < DateTime.Now)
            {
                return Json(new
                {
                    success = false,
                    message = "Mã đã hết hạn"
                });
            }

            bool daDung = db.DonHang.Any(x =>
                x.MaNguoiDung == maND &&
                x.MaKhuyenMai == voucher.MaKhuyenMai);

            if (daDung)
            {
                return Json(new
                {
                    success = false,
                    message = "Bạn đã sử dụng mã này rồi"
                });
            }

            decimal tienGiam =
                (decimal)(tongTien * voucher.PhanTramGiam / 100);

            decimal tongSauGiam =
                tongTien - tienGiam;

            return Json(new
            {
                success = true,
                phanTram = voucher.PhanTramGiam,
                tienGiam = tienGiam,
                tongSauGiam = tongSauGiam,
                maKhuyenMai = voucher.MaKhuyenMai,
                message = "Áp dụng mã thành công (-" +
                          voucher.PhanTramGiam + "%)"
            });
        }
        [HttpPost]

        public ActionResult DatHang(
string HoTen,
string SoDienThoai,
string Email,
string DiaChi,
int MaPhuongThuc,
decimal TongTien,
string ids = null,        // danh sách ID sản phẩm được chọn
string MaGiamGia = null)
        {
            if (Session["MaNguoiDung"] == null)
                return RedirectToAction("TrangChu", "Home");

            int maND = Convert.ToInt32(Session["MaNguoiDung"]);

            List<GioHang> gioHang = new List<GioHang>();

            // ================= XỬ LÝ MUA NGAY =================
            if (Request.Form["MuaNgay"] == "true")
            {
                if (int.TryParse(Request.Form["MaSanPham"], out int maSP) &&
                    int.TryParse(Request.Form["SoLuong"], out int soLuong))
                {
                    var sp = db.SanPhams.Find(maSP);
                    if (sp != null)
                    {
                        gioHang.Add(new GioHang
                        {
                            MaSanPham = maSP,
                            SoLuong = soLuong,
                            SanPham = sp
                        });
                    }
                }
            }
            else
            {
                // ================= LẤY SẢN PHẨM TỪ GIỎ HÀNG =================
                var query = db.GioHangs
                              .Include(x => x.SanPham)
                              .Where(x => x.MaNguoiDung == maND);

                if (!string.IsNullOrEmpty(ids))
                {
                    var listId = ids.Split(',')
                                    .Select(x => int.TryParse(x.Trim(), out int id) ? id : 0)
                                    .Where(x => x > 0)
                                    .ToList();

                    query = query.Where(x => x.MaSanPham.HasValue && listId.Contains(x.MaSanPham.Value));
                }

                gioHang = query.ToList();
            }

            if (!gioHang.Any())
            {
                TempData["Error"] = "Không có sản phẩm nào để đặt hàng!";
                return RedirectToAction("GioHang", "Home");
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int? maKhuyenMai = null;
                    if (!string.IsNullOrEmpty(MaGiamGia))
                    {
                        var voucher = db.KhuyenMais.FirstOrDefault(x => x.MaCode == MaGiamGia);
                        if (voucher != null)
                            maKhuyenMai = voucher.MaKhuyenMai;
                    }

                    // Tạo đơn hàng
                    var donHang = new DonHang
                    {
                        MaNguoiDung = maND,
                        TongTien = TongTien,
                        TrangThai = "Chờ xác nhận",
                        DiaChiGiao = DiaChi,
                        NgayDat = DateTime.Now,
                        MaPhuongThuc = MaPhuongThuc,
                        MaKhuyenMai = maKhuyenMai
                    };

                    db.DonHangs.Add(donHang);
                    db.SaveChanges();

                    // Thêm chi tiết đơn hàng + trừ tồn kho
                    foreach (var item in gioHang)
                    {
                        var chiTietSP = db.ChiTietSanPhams
                            .FirstOrDefault(x => x.MaSanPham == item.MaSanPham);

                        if (chiTietSP == null) continue;

                        int soLuong = item.SoLuong ?? 1;

                        // ❌ KHÔNG TRỪ KHO Ở ĐÂY

                        db.ChiTietDonHangs.Add(new ChiTietDonHang
                        {
                            MaDonHang = donHang.MaDonHang,
                            MaChiTietSanPham = chiTietSP.MaChiTiet,
                            SoLuong = soLuong,
                            Gia = item.SanPham?.Gia ?? 0
                        });
                    }

                    // ================= KHÔNG XÓA SẢN PHẨM KHỎI GIỎ HÀNG =================
                    // (Bỏ hoàn toàn phần RemoveRange)

                    db.SaveChanges();
                    transaction.Commit();

                    // Redirect theo phương thức thanh toán
                    if (MaPhuongThuc == 2)
                        return RedirectToAction("QR", new { maDonHang = donHang.MaDonHang });
                    else if (MaPhuongThuc == 5)
                        return RedirectToAction("ThanhCongCOD", new { maDonHang = donHang.MaDonHang });

                    TempData["Success"] = "Đặt hàng thành công! Sản phẩm vẫn giữ trong giỏ hàng.";
                    return RedirectToAction("DonHangCuaToi", "Home");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["Error"] = "Đặt hàng thất bại: " + ex.Message;
                    return RedirectToAction("GioHang", "Home");
                }
            }
        }
        public ActionResult QR(int maDonHang)
        {
            var donHang = db.DonHangs.FirstOrDefault(x => x.MaDonHang == maDonHang);

            if (donHang == null)
                return RedirectToAction("GioHang");

            decimal tongTien = donHang.TongTien ?? 0;
            decimal tienCoc = tongTien * 0.1m;

            string stk = "123456789";
            string bank = "VCB";
            string ten = "LE TUAN MANH";
            string noiDung = "DH" + maDonHang;

            string qr =
                $"https://img.vietqr.io/image/{bank}-{stk}-compact2.png" +
                $"?amount={(long)tienCoc}" +
                $"&addInfo={noiDung}" +
                $"&accountName={ten}";

            // ✅ THÊM: thời gian hết hạn (5 phút)
            DateTime expireTime = DateTime.Now.AddMinutes(5);

            // convert sang milliseconds
            long expireTimestamp = new DateTimeOffset(expireTime).ToUnixTimeMilliseconds();

            ViewBag.ExpireTime = expireTimestamp;

            ViewBag.MaDonHang = maDonHang;
            ViewBag.QR = qr;
            ViewBag.TienCoc = tienCoc;
            ViewBag.TongTien = tongTien;
            ViewBag.NoiDung = noiDung;

            return View();
        }
        [HttpPost]
        public JsonResult XacNhanChuyenKhoan(int maDonHang)
        {
            var donHang = db.DonHangs.FirstOrDefault(x => x.MaDonHang == maDonHang);

            if (donHang == null)
                return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

            var chiTietDH = db.ChiTietDonHangs
                .Where(x => x.MaDonHang == maDonHang)
                .ToList();

            foreach (var item in chiTietDH)
            {
                var chiTietSP = db.ChiTietSanPhams
                    .FirstOrDefault(x => x.MaChiTiet == item.MaChiTietSanPham);

                if (chiTietSP == null)
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });

                if ((chiTietSP.SoLuongTon ?? 0) < item.SoLuong)
                    return Json(new { success = false, message = "Sản phẩm không đủ hàng" });

                // ✅ TRỪ KHO Ở ĐÂY MỚI ĐÚNG
                chiTietSP.SoLuongTon -= item.SoLuong;
            }

            // ✅ cập nhật trạng thái đơn
            donHang.TrangThai = "Đã xác nhận";

            db.ThanhToans.Add(new ThanhToan
            {
                MaDonHang = maDonHang,
                MaPhuongThuc = 2,
                SoTien = donHang.TongTien ?? 0,
                TrangThai = "Hoàn thành",
                MaGiaoDich = Guid.NewGuid().ToString(),
                NgayThanhToan = DateTime.Now
            });

            db.SaveChanges();

            return Json(new { success = true });
        }
        [HttpPost]
        public JsonResult HuyDonHang(int maDonHang)
        {
            var don = db.DonHangs.Find(maDonHang);

            if (don == null)
                return Json(new { success = false });

            don.TrangThai = "Đã hủy"; // hoặc = 0
            db.SaveChanges();

            return Json(new { success = true });
        }
        public ActionResult ThanhCongCOD(int maDonHang)
        {
            var donHang = db.DonHangs.FirstOrDefault(x => x.MaDonHang == maDonHang);

            if (donHang == null)
                return RedirectToAction("GioHang");

            decimal tongTien = donHang.TongTien ?? 0;
            decimal tienCoc = tongTien * 0.1m;

            string stk = "123456789";
            string bank = "VCB";
            string ten = "LE TUAN MANH";
            string noiDung = "DH" + maDonHang;

            string qr =
                $"https://img.vietqr.io/image/{bank}-{stk}-compact2.png" +
                $"?amount={(long)tienCoc}" +
                $"&addInfo={noiDung}" +
                $"&accountName={ten}";

            ViewBag.MaDonHang = maDonHang;
            ViewBag.TongTien = tongTien;
            ViewBag.TienCoc = tienCoc;

            // ✅ THÊM 2 DÒNG NÀY
            ViewBag.QR = qr;
            ViewBag.NoiDung = noiDung;

            return View();
        }
        public ActionResult MuaNgay(int id, int soLuong)
        {
            if (Session["MaNguoiDung"] == null)
                return RedirectToAction("TrangChu", "Home");

            if (soLuong < 1) soLuong = 1;

            var sp = db.SanPhams.Find(id);
            if (sp == null)
                return HttpNotFound();

            // Tạo giỏ tạm (KHÔNG dùng DB)
            var cart = new List<GioHang>
    {
        new GioHang
        {
            MaSanPham = id,
            SoLuong = soLuong,
            SanPham = sp
        }
    };

            ViewBag.MuaNgay = true; // để phân biệt với giỏ hàng

            LoadMenu();
            return View("ThanhToan", cart);
        }
        public ActionResult ThanhToan(string ids)
        {
            if (Session["MaNguoiDung"] == null)
                return RedirectToAction("TrangChu", "Home");

            int maND = (int)Session["MaNguoiDung"];

            // =========================
            // 🔥 MUA NGAY
            // =========================
            var muaNgay = Session["MuaNgay"];
            if (muaNgay != null)
            {
                dynamic data = muaNgay;

                int maSP = data.MaSanPham;
                int soLuong = data.SoLuong;

                var sp = db.SanPhams.Find(maSP);
                if (sp == null)
                    return RedirectToAction("TrangChu");

                var fakeCart = new List<GioHang>
        {
            new GioHang
            {
                MaSanPham = maSP,
                SoLuong = soLuong,
                SanPham = sp
            }
        };

                ViewBag.Ids = maSP.ToString();
                ViewBag.IsMuaNgay = true;

                return View(fakeCart);
            }

            // =========================
            // 🛒 GIỎ HÀNG
            // =========================
            List<int> listId;

            if (string.IsNullOrWhiteSpace(ids))
            {
                listId = db.GioHangs
                    .Where(x => x.MaNguoiDung == maND && x.MaSanPham != null)
                    .Select(x => x.MaSanPham.Value)
                    .ToList();
            }
            else
            {
                listId = ids.Split(',')
                    .Select(x => int.Parse(x))
                    .ToList();
            }

            var cart = db.GioHangs
                .Include(x => x.SanPham)
                .Where(x => x.MaNguoiDung == maND
                         && listId.Contains(x.MaSanPham.Value))
                .ToList();

            if (!cart.Any())
            {
                TempData["Error"] = "Giỏ hàng trống.";
                return RedirectToAction("GioHang");
            }

            ViewBag.Ids = string.Join(",", listId);
            ViewBag.IsMuaNgay = false;

            return View(cart);
        }
        public ActionResult DonHangCuaToi(string trangThai = "all")
        {
            AutoConfirmOrder();

            if (Session["MaNguoiDung"] == null)
                return RedirectToAction("DangNhap", "Home");

            int maND = Convert.ToInt32(Session["MaNguoiDung"]);

            var donHangs = db.DonHangs
       .Include("ChiTietDonHangs.ChiTietSanPham.SanPham")
       .Where(x => x.MaNguoiDung == maND)
       .OrderByDescending(x => x.NgayDat)
       .ToList();
            if (trangThai != "all")
            {
                if (trangThai == "HoanThanh")
                {
                    donHangs = donHangs
                        .Where(x => x.TrangThai == "Hoàn thành")
                        .ToList();
                }
                else
                {
                    donHangs = donHangs
                        .Where(x => x.TrangThai == trangThai)
                        .ToList();
                }
            }

            LoadMenu();
            return View(donHangs);
        }

        [HttpPost]
        public JsonResult DaNhanHang(int id)
        {
            var dh = db.DonHangs.Find(id);

            if (dh == null)
                return Json(new { success = false });

            dh.TrangThai = "Đã giao hàng";
            dh.NgayGiaoHang = DateTime.Now;
            db.SaveChanges();

            return Json(new { success = true });
        }
        void AutoConfirmOrder()
        {
            var now = DateTime.Now;

            var list = db.DonHangs
                .Where(x => x.TrangThai == "Đang giao"
                         && x.NgayGiaoHang != null)
                .ToList();

            foreach (var dh in list)
            {
                if ((now - dh.NgayGiaoHang.Value).TotalMinutes >= 10)
                {
                    dh.TrangThai = "Hoàn thành";
                }
            }

            db.SaveChanges();
        }
        public ActionResult danhgiasanpham(int maSP)
        {
            if (Session["MaNguoiDung"] == null)
                return RedirectToAction("DangNhap", "Home");

            int maND = (int)Session["MaNguoiDung"];

            // 👉 kiểm tra đã đánh giá chưa
            var danhGia = db.DanhGias
                .FirstOrDefault(x => x.MaNguoiDung == maND && x.MaSanPham == maSP);

            ViewBag.MaSanPham = maSP;
            ViewBag.DanhGia = danhGia; // null = chưa đánh giá

            return View();
        }

        [HttpPost]
        public ActionResult danhgiasanpham(DanhGia dg)
        {
            if (Session["MaNguoiDung"] == null)
                return RedirectToAction("DangNhap", "Home");

            int maND = (int)Session["MaNguoiDung"];

            // 👉 kiểm tra đã đánh giá chưa
            var existing = db.DanhGias
                .FirstOrDefault(x => x.MaNguoiDung == maND && x.MaSanPham == dg.MaSanPham);

            if (existing != null)
            {
                // ✅ UPDATE (sửa đánh giá)
                existing.SoSao = dg.SoSao;
                existing.NoiDung = dg.NoiDung;
                existing.NgayDanhGia = DateTime.Now;
            }
            else
            {
                // ✅ INSERT (đánh giá lần đầu)
                dg.MaNguoiDung = maND;
                dg.NgayDanhGia = DateTime.Now;

                db.DanhGias.Add(dg);
            }

            db.SaveChanges();

            return RedirectToAction("DonHangCuaToi");
        }
    }
}
