using DoAnTotNghiep.Models;
// Thay bằng namespace của bạn
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Dynamic;
using System.Linq;
using System.Web.Mvc;

namespace DoAnTotNghiep.Controllers
{
    public class AdminController : Controller
    {
        private Model1 db = new Model1();

        // Load danh mục cho dropdown

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Session["RoleId"] == null || (int)Session["RoleId"] != 1)
            {
                filterContext.Result = new RedirectResult("~/Home/Login");
                return;
            }
            base.OnActionExecuting(filterContext);
        }
        public ActionResult admin()
        {
            if (Session["RoleId"] == null || (int)Session["RoleId"] != 1)
                return RedirectToAction("Login", "Home");
            {
                var db = new Model1(); // hoặc DbContext của bạn

                ViewBag.SoSanPham = db.SanPhams.Count();
                ViewBag.SoNguoiDung = db.NguoiDungs.Count();
                ViewBag.SoDonHang = db.DonHangs.Count();
                ViewBag.DoanhThu = db.DonHangs.Sum(x => (decimal?)x.TongTien) ?? 0;

                return View();
            }
        }
        // ====================== TỔNG QUAN DASHBOARD ======================
        // ====================== TỔNG QUAN DASHBOARD ======================
        // ====================== TỔNG QUAN DASHBOARD ======================
   
        public ActionResult QuanLySanPham(string search, int page = 1, int pageSize = 10)
        {

            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = 10; // Giới hạn pageSize

                var query = db.SanPhams
                    .Include(s => s.DanhMuc)
                    .Include(s => s.ChiTietSanPhams)                    // Quan trọng để lấy tồn kho
                    .Include(s => s.ChiTietSanPhams.Select(ct => ct.ThuongHieu))
                    .Include(s => s.ChiTietSanPhams.Select(ct => ct.ChatLieu))
                    .Include(s => s.ChiTietSanPhams.Select(ct => ct.MauSac))
                    .AsQueryable();

                // === TÌM KIẾM ===
                // === TÌM KIẾM ===
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower().Trim();

                    query = query.Where(s =>
                        s.TenSanPham.ToLower().Contains(search) ||                    // Tìm theo tên sản phẩm
                        (s.DanhMuc != null && s.DanhMuc.TenDanhMuc.ToLower().Contains(search))  // Tìm theo danh mục
                    );
                }

                // === ĐẾM TỔNG SỐ (Tách riêng để tối ưu) ===
                int totalProducts = query.Count();   // Chỉ count, không lấy data

                int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

                // === LẤY DỮ LIỆU PHÂN TRANG ===
                var result = query
                    .OrderByDescending(s => s.NoiBat)
                    .ThenByDescending(s => s.NgayTao)
                    .ThenByDescending(s => s.MaSanPham)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // === TRUYỀN DỮ LIỆU SANG VIEW ===
                ViewBag.TotalPages = totalPages;
                ViewBag.CurrentPage = page;
                ViewBag.Search = search;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalProducts = totalProducts;
                ViewBag.DanhMucList = new SelectList(db.DanhMucs.ToList(), "MaDanhMuc", "TenDanhMuc");

                return View(result);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi tải danh sách sản phẩm: " + ex.Message;
                if (ex.InnerException != null)
                    TempData["ErrorMessage"] += " - Chi tiết: " + ex.InnerException.Message;

                // Trả về list rỗng để tránh crash View
                ViewBag.TotalPages = 0;
                ViewBag.CurrentPage = 1;
                ViewBag.TotalProducts = 0;

                return View(new List<SanPham>());
            }
        }
        // ====================== THÊM SẢN PHẨM ======================
        // ====================== THÊM SẢN PHẨM ======================
        // ====================== THÊM SẢN PHẨM ======================
        // ====================== THÊM SẢN PHẨM ======================
        public ActionResult ThemSanPham()
        {
            ViewBag.DanhMucList = new SelectList(db.DanhMucs.ToList(), "MaDanhMuc", "TenDanhMuc");
            ViewBag.ThuongHieuList = new SelectList(db.ThuongHieux.ToList(), "MaThuongHieu", "TenThuongHieu");
            ViewBag.ChatLieuList = new SelectList(db.ChatLieux.ToList(), "MaChatLieu", "TenChatLieu");
            ViewBag.MauSacList = new SelectList(db.MauSacs.ToList(), "MaMau", "TenMau");

            return View(new SanPham());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemSanPham(SanPham model, int MaThuongHieu, int MaChatLieu, int MaMau, int SoLuongTon = 10)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Thêm SanPham trước
                    db.SanPhams.Add(model);
                    db.SaveChanges();

                    // 2. Thêm ChiTietSanPham
                    var chiTiet = new ChiTietSanPham
                    {
                        MaSanPham = model.MaSanPham,
                        MaThuongHieu = MaThuongHieu,
                        MaChatLieu = MaChatLieu,
                        MaMau = MaMau,
                        SoLuongTon = SoLuongTon > 0 ? SoLuongTon : 10   // tránh số âm
                    };

                    db.ChiTietSanPhams.Add(chiTiet);
                    db.SaveChanges();

                    TempData["Message"] = "Thêm sản phẩm thành công!";
                    return RedirectToAction("QuanLySanPham");
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Lỗi khi thêm sản phẩm: " + ex.Message;
                    if (ex.InnerException != null)
                        ViewBag.Error += " - " + ex.InnerException.Message;
                }
            }

            // Load lại dropdown khi có lỗi
            ViewBag.DanhMucList = new SelectList(db.DanhMucs.ToList(), "MaDanhMuc", "TenDanhMuc");
            ViewBag.ThuongHieuList = new SelectList(db.ThuongHieux.ToList(), "MaThuongHieu", "TenThuongHieu");
            ViewBag.ChatLieuList = new SelectList(db.ChatLieux.ToList(), "MaChatLieu", "TenChatLieu");
            ViewBag.MauSacList = new SelectList(db.MauSacs.ToList(), "MaMau", "TenMau");

            return View(model);
        }
        // ====================== CHỈNH SỬA SẢN PHẨM ======================
        // GET: Hiển thị form chỉnh sửa
        public ActionResult ChinhSuaSanPham(int id)
        {
            try
            {
                var sanPham = db.SanPhams
                    .Include(s => s.DanhMuc)   // Để hiển thị tên danh mục nếu cần
                    .FirstOrDefault(s => s.MaSanPham == id);

                if (sanPham == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                    return RedirectToAction("QuanLySanPham");
                }

                // Chuẩn bị dữ liệu cho các dropdown
                ViewBag.DanhMucList = new SelectList(db.DanhMucs.ToList(), "MaDanhMuc", "TenDanhMuc", sanPham.MaDanhMuc);
                ViewBag.ThuongHieuList = new SelectList(db.ThuongHieux.ToList(), "MaThuongHieu", "TenThuongHieu");
                ViewBag.ChatLieuList = new SelectList(db.ChatLieux.ToList(), "MaChatLieu", "TenChatLieu");
                ViewBag.MauSacList = new SelectList(db.MauSacs.ToList(), "MaMau", "TenMau");

                // Lấy thông tin ChiTietSanPham hiện tại (để hiển thị Thương hiệu, Chất liệu, Màu, Số lượng tồn)
                var chiTiet = db.ChiTietSanPhams
                    .FirstOrDefault(ct => ct.MaSanPham == id);

                if (chiTiet != null)
                {
                    ViewBag.MaThuongHieu = chiTiet.MaThuongHieu;
                    ViewBag.MaChatLieu = chiTiet.MaChatLieu;
                    ViewBag.MaMau = chiTiet.MaMau;
                    ViewBag.SoLuongTon = chiTiet.SoLuongTon;
                }
                else
                {
                    // Giá trị mặc định nếu chưa có chi tiết
                    ViewBag.MaThuongHieu = null;
                    ViewBag.MaChatLieu = null;
                    ViewBag.MaMau = null;
                    ViewBag.SoLuongTon = 0;
                }

                return View(sanPham);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi tải sản phẩm: " + ex.Message;
                if (ex.InnerException != null)
                    TempData["ErrorMessage"] += " - Chi tiết: " + ex.InnerException.Message;

                return RedirectToAction("QuanLySanPham");
            }
        }

        // POST: Xử lý chỉnh sửa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChinhSuaSanPham(SanPham model, int? MaThuongHieu, int? MaChatLieu, int? MaMau, int? SoLuongTon)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var sanPham = db.SanPhams.Find(model.MaSanPham);
                    if (sanPham == null)
                    {
                        ViewBag.Error = "Không tìm thấy sản phẩm để chỉnh sửa.";
                        return View(model);
                    }

                    // Cập nhật thông tin SanPham
                    sanPham.TenSanPham = model.TenSanPham;
                    sanPham.Gia = model.Gia;
                    sanPham.MoTa = model.MoTa;
                    sanPham.AnhChinh = model.AnhChinh;
                    sanPham.AnhPhu1 = model.AnhPhu1;
                    sanPham.AnhPhu2 = model.AnhPhu2;
                    sanPham.AnhPhu3 = model.AnhPhu3;
                    sanPham.MaDanhMuc = model.MaDanhMuc;
                    sanPham.NoiBat = model.NoiBat;

                    // Cập nhật ChiTietSanPham (chỉ có 1 chi tiết theo thiết kế hiện tại của bạn)
                    var chiTiet = db.ChiTietSanPhams.FirstOrDefault(ct => ct.MaSanPham == model.MaSanPham);

                    if (chiTiet != null)
                    {
                        if (MaThuongHieu.HasValue) chiTiet.MaThuongHieu = MaThuongHieu.Value;
                        if (MaChatLieu.HasValue) chiTiet.MaChatLieu = MaChatLieu.Value;
                        if (MaMau.HasValue) chiTiet.MaMau = MaMau.Value;
                        if (SoLuongTon.HasValue) chiTiet.SoLuongTon = SoLuongTon.Value;
                    }
                    else
                    {
                        // Trường hợp chưa có chi tiết (ít xảy ra)
                        chiTiet = new ChiTietSanPham
                        {
                            MaSanPham = model.MaSanPham,
                            MaThuongHieu = MaThuongHieu ?? 1,
                            MaChatLieu = MaChatLieu ?? 1,
                            MaMau = MaMau ?? 1,
                            SoLuongTon = SoLuongTon ?? 10
                        };
                        db.ChiTietSanPhams.Add(chiTiet);
                    }

                    db.SaveChanges();

                    TempData["Message"] = "Chỉnh sửa sản phẩm thành công!";
                    return RedirectToAction("QuanLySanPham");
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Lỗi khi chỉnh sửa sản phẩm: " + ex.Message;
                    if (ex.InnerException != null)
                        ViewBag.Error += " - Chi tiết: " + ex.InnerException.Message;
                }
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                ViewBag.Error = "Dữ liệu không hợp lệ: " + string.Join(", ", errors);
            }

            // Nếu có lỗi, load lại dropdown
            ViewBag.DanhMucList = new SelectList(db.DanhMucs.ToList(), "MaDanhMuc", "TenDanhMuc", model.MaDanhMuc);
            ViewBag.ThuongHieuList = new SelectList(db.ThuongHieux.ToList(), "MaThuongHieu", "TenThuongHieu", MaThuongHieu);
            ViewBag.ChatLieuList = new SelectList(db.ChatLieux.ToList(), "MaChatLieu", "TenChatLieu", MaChatLieu);
            ViewBag.MauSacList = new SelectList(db.MauSacs.ToList(), "MaMau", "TenMau", MaMau);

            return View(model);
        }

        // ====================== XÓA SẢN PHẨM ======================
        [HttpPost]

        public ActionResult XoaSanPham(int id)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // 1. Kiểm tra sản phẩm tồn tại
                    var sanPham = db.SanPhams.Find(id);
                    if (sanPham == null)
                    {
                        TempData["Message"] = "Không tìm thấy sản phẩm để xóa!";
                        return RedirectToAction("QuanLySanPham");
                    }

                    // ================== XÓA THEO THỨ TỰ ĐÚNG (từ con → cha) ==================

                    // A. Xóa ChiTietGioHang liên quan đến sản phẩm này
                    var chiTietGioHangs = db.ChiTietGioHangs
                        .Where(cgh => db.ChiTietSanPhams
                            .Any(ct => ct.MaChiTiet == cgh.MaChiTietSanPham && ct.MaSanPham == id))
                        .ToList();

                    if (chiTietGioHangs.Any())
                        db.ChiTietGioHangs.RemoveRange(chiTietGioHangs);

                    // B. Xóa ChiTietDonHang liên quan
                    var chiTietDonHangs = db.ChiTietDonHangs
                        .Where(cdh => db.ChiTietSanPhams
                            .Any(ct => ct.MaChiTiet == cdh.MaChiTietSanPham && ct.MaSanPham == id))
                        .ToList();

                    if (chiTietDonHangs.Any())
                        db.ChiTietDonHangs.RemoveRange(chiTietDonHangs);

                    // C. Xóa DanhGia của sản phẩm
                    var danhGias = db.DanhGias.Where(d => d.MaSanPham == id).ToList();
                    if (danhGias.Any())
                        db.DanhGias.RemoveRange(danhGias);

                    // D. Xóa ChiTietSanPham (rất quan trọng - phải xóa trước SanPham)
                    var chiTietSanPhams = db.ChiTietSanPhams.Where(ct => ct.MaSanPham == id).ToList();
                    if (chiTietSanPhams.Any())
                        db.ChiTietSanPhams.RemoveRange(chiTietSanPhams);

                    // E. Cuối cùng mới xóa SanPham chính
                    db.SanPhams.Remove(sanPham);

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["Message"] = $"Xóa sản phẩm '{sanPham.TenSanPham}' thành công!";
                    TempData["MessageType"] = "success";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    string errorMsg = "Lỗi khi xóa sản phẩm: " + ex.Message;
                    if (ex.InnerException != null)
                    {
                        errorMsg += " - Chi tiết: " + ex.InnerException.Message;

                        // Lỗi phổ biến: Foreign Key Constraint
                        if (ex.InnerException.Message.Contains("REFERENCE") ||
                            ex.InnerException.Message.Contains("constraint"))
                        {
                            errorMsg = "Không thể xóa sản phẩm vì vẫn còn đơn hàng hoặc giỏ hàng liên quan. " +
                                       "Vui lòng kiểm tra lại!";
                        }
                    }

                    TempData["Message"] = errorMsg;
                    TempData["MessageType"] = "danger";
                }
            }

            return RedirectToAction("QuanLySanPham");
        }
        // ====================== QUẢN LÝ BANNER ======================

        // GET: Danh sách Banner
        public ActionResult QuanLyBanner(string search = "", int page = 1, int pageSize = 10)
        {
            var query = db.Banners.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower().Trim();
                query = query.Where(b =>
                    b.TieuDe.ToLower().Contains(search) ||
                    (b.ViTri != null && b.ViTri.ToLower().Contains(search))
                );
            }

            int total = query.Count();
            int totalPages = (int)Math.Ceiling((double)total / pageSize);

            var banners = query
                .OrderByDescending(b => b.ThuTu)
                .ThenByDescending(b => b.NgayBatDau)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.Search = search;
            ViewBag.TotalBanners = total;

            return View(banners);
        }

        // GET: Thêm Banner
        public ActionResult ThemBanner()
        {
            return View();
        }

        // POST: Thêm Banner
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemBanner(Banner model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    model.NgayBatDau = model.NgayBatDau ?? DateTime.Now;
                    model.NgayKetThuc = model.NgayKetThuc ?? DateTime.Now.AddMonths(1);
                    model.TrangThai = model.TrangThai ?? true;

                    db.Banners.Add(model);
                    db.SaveChanges();

                    TempData["Message"] = "Thêm banner thành công!";
                    return RedirectToAction("QuanLyBanner");
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Lỗi khi thêm banner: " + ex.Message;
                }
            }
            return View(model);
        }

        // GET: Chỉnh sửa Banner
        public ActionResult ChinhSuaBanner(int id)
        {
            var banner = db.Banners.Find(id);
            if (banner == null) return HttpNotFound();

            return View(banner);
        }

        // POST: Chỉnh sửa Banner
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChinhSuaBanner(Banner model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var banner = db.Banners.Find(model.MaBanner);
                    if (banner == null) return HttpNotFound();

                    banner.TieuDe = model.TieuDe;
                    banner.HinhAnh = model.HinhAnh;
                    banner.Link = model.Link;
                    banner.ViTri = model.ViTri;
                    banner.TrangThai = model.TrangThai;
                    banner.ThuTu = model.ThuTu;
                    banner.NgayBatDau = model.NgayBatDau;
                    banner.NgayKetThuc = model.NgayKetThuc;

                    db.SaveChanges();
                    TempData["Message"] = "Cập nhật banner thành công!";
                    return RedirectToAction("QuanLyBanner");
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Lỗi khi cập nhật: " + ex.Message;
                }
            }
            return View(model);
        }

        // POST: Xóa Banner
        [HttpPost]

        public ActionResult XoaBanner(int id)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var banner = db.Banners.Find(id);

                    if (banner == null)
                    {
                        TempData["Message"] = "Không tìm thấy banner!";
                        TempData["MessageType"] = "warning";
                        return RedirectToAction("QuanLyBanner");
                    }

                    db.Banners.Remove(banner);
                    db.SaveChanges();

                    transaction.Commit();

                    TempData["Message"] = $"Xóa banner '{banner.TieuDe}' thành công!";
                    TempData["MessageType"] = "success";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    string errorMsg = "Lỗi khi xóa banner: " + ex.Message;

                    if (ex.InnerException != null)
                    {
                        errorMsg += " | " + ex.InnerException.Message;
                    }

                    TempData["Message"] = errorMsg;
                    TempData["MessageType"] = "danger";
                }
            }

            return RedirectToAction("QuanLyBanner");
        }
        public ActionResult DanhSachTaiKhoan(string search, int page = 1, int pageSize = 10)
        {
            if (Session["RoleId"] == null || Convert.ToInt32(Session["RoleId"]) != 1)
            {
                TempData["Message"] = "Bạn cần quyền Admin để truy cập trang này!";
                return RedirectToAction("Login", "Admin");
            }

            var users = db.NguoiDungs.Include(u => u.VaiTro).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                users = users.Where(u =>
                    u.Ten.ToLower().Contains(search) ||
                    u.Email.ToLower().Contains(search) ||
                    (u.SoDienThoai != null && u.SoDienThoai.Contains(search))
                );
            }

            int total = users.Count();
            int totalPages = (int)Math.Ceiling((double)total / pageSize);

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.Search = search;

            var model = users
                .OrderByDescending(u => u.MaNguoiDung)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return View(model);
        }

        // ====================== THÊM TÀI KHOẢN ======================
        public ActionResult ThemTaiKhoan()
        {
            if (Session["RoleId"] == null || Convert.ToInt32(Session["RoleId"]) != 1)
            {
                TempData["Message"] = "Bạn cần quyền Admin!";
                return RedirectToAction("Login", "Admin");
            }

            // SỬA LỖI Ở ĐÂY: Dùng VaiTro thay vì Roles
            // Dùng đúng tên DbSet là VaiTros
            ViewBag.VaiTroList = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Admin" },
                new SelectListItem { Value = "2", Text = "Khách hàng" },
                new SelectListItem { Value = "3", Text = "Nhân viên bán hàng" },
                new SelectListItem { Value = "4", Text = "Quản lý kho" },
                new SelectListItem { Value = "5", Text = "Quản trị viên" },
                new SelectListItem { Value = "6", Text = "Khách VIP" }
            };
            return View(new NguoiDung());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemTaiKhoan(NguoiDung model, string MatKhau)
        {
            if (Session["RoleId"] == null || Convert.ToInt32(Session["RoleId"]) != 1)
            {
                return RedirectToAction("Login", "Admin");
            }

            // Khởi tạo lại danh sách vai trò
            ViewBag.VaiTroList = new List<SelectListItem>
    {
        new SelectListItem { Value = "1", Text = "Admin" },
        new SelectListItem { Value = "2", Text = "Khách hàng" },
        new SelectListItem { Value = "3", Text = "Nhân viên bán hàng" },
        new SelectListItem { Value = "4", Text = "Quản lý kho" },
        new SelectListItem { Value = "5", Text = "Quản trị viên" },
        new SelectListItem { Value = "6", Text = "Khách VIP" }
    };

            // Kiểm tra validation
            if (string.IsNullOrEmpty(MatKhau))
            {
                ModelState.AddModelError("", "Mật khẩu không được để trống");
                return View(model);
            }

            if (model.MaVaiTro == null || model.MaVaiTro == 0)
            {
                ModelState.AddModelError("MaVaiTro", "Vui lòng chọn vai trò!");
                return View(model);
            }

            if (db.NguoiDungs.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email đã tồn tại!");
                return View(model);
            }

            try
            {
                model.MatKhau = MatKhau;        // Nên mã hóa sau này (BCrypt hoặc Hash)
                model.NgayTao = DateTime.Now;

                db.NguoiDungs.Add(model);
                db.SaveChanges();

                TempData["Success"] = "Thêm tài khoản thành công!";
                return RedirectToAction("DanhSachTaiKhoan");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
                return View(model);
            }
        }
        // ====================== CHỈNH SỬA TÀI KHOẢN ======================
        public ActionResult ChinhSuaTaiKhoan(int? id)
        {
            if (Session["RoleId"] == null || Convert.ToInt32(Session["RoleId"]) != 1)
                return RedirectToAction("Login", "Admin");

            if (id == null) return HttpNotFound();

            var user = db.NguoiDungs.Find(id);
            if (user == null) return HttpNotFound();

            ViewBag.VaiTroList = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Admin" },
                new SelectListItem { Value = "2", Text = "Khách hàng" },
                new SelectListItem { Value = "3", Text = "Nhân viên bán hàng" },
                new SelectListItem { Value = "4", Text = "Quản lý kho" },
                new SelectListItem { Value = "5", Text = "Quản trị viên" },
                new SelectListItem { Value = "6", Text = "Khách VIP" }
            };

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChinhSuaTaiKhoan(NguoiDung model)
        {
            if (Session["RoleId"] == null || Convert.ToInt32(Session["RoleId"]) != 1)
                return RedirectToAction("Login", "Admin");

            if (ModelState.IsValid)
            {
                var user = db.NguoiDungs.Find(model.MaNguoiDung);
                if (user != null)
                {
                    user.Ten = model.Ten;
                    user.Email = model.Email;
                    user.SoDienThoai = model.SoDienThoai;
                    user.DiaChi = model.DiaChi;
                    user.MaVaiTro = model.MaVaiTro;

                    db.SaveChanges();
                    TempData["Success"] = "Cập nhật tài khoản thành công!";
                    return RedirectToAction("DanhSachTaiKhoan");
                }
            }

            ViewBag.VaiTroList = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Admin" },
                new SelectListItem { Value = "2", Text = "Khách hàng" },
                new SelectListItem { Value = "3", Text = "Nhân viên bán hàng" },
                new SelectListItem { Value = "4", Text = "Quản lý kho" },
                new SelectListItem { Value = "5", Text = "Quản trị viên" },
                new SelectListItem { Value = "6", Text = "Khách VIP" }
            };
            TempData["Error"] = "Dữ liệu không hợp lệ!";
            return View(model);
        }

        // ====================== XÓA TÀI KHOẢN ======================
        [HttpPost]

        public ActionResult XoaTaiKhoan(int id)
        {
            if (Session["RoleId"] == null || Convert.ToInt32(Session["RoleId"]) != 1)
            {
                TempData["Message"] = "Bạn cần quyền Admin!";
                return RedirectToAction("Login", "Admin");
            }

            var user = db.NguoiDungs
                .Include(u => u.GioHangs)
                .Include(u => u.DonHangs)
                .Include(u => u.DanhGias)
                .FirstOrDefault(u => u.MaNguoiDung == id);

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản!";
                return RedirectToAction("DanhSachTaiKhoan");
            }

            try
            {
                if (user.GioHangs != null) db.GioHangs.RemoveRange(user.GioHangs);
                if (user.DonHangs != null) db.DonHangs.RemoveRange(user.DonHangs);
                if (user.DanhGias != null) db.DanhGias.RemoveRange(user.DanhGias);

                db.NguoiDungs.Remove(user);
                db.SaveChanges();

                TempData["Success"] = "Xóa tài khoản thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi xóa: " + ex.Message;
            }

            return RedirectToAction("DanhSachTaiKhoan");
        }
       
        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}