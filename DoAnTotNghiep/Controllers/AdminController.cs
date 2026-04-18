using DoAnTotNghiep.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;

namespace DoAnTotNghiep.Controllers
{
    public class AdminController : Controller
    {
        private Model1 db = new Model1();

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Session["VaiTro"] == null || (int)Session["VaiTro"] != 1)
            {
                filterContext.Result = new RedirectResult("~/Home/Login");
                return;
            }
            base.OnActionExecuting(filterContext);
        }
        public ActionResult admin()
        {
            if (Session["VaiTro"] == null || (int)Session["VaiTro"] != 1)
                return RedirectToAction("Login", "Home");
            {
                var db = new Model1(); // hoặc DbContext của bạn

                ViewBag.SoSanPham = db.SanPham.Count();
                ViewBag.SoNguoiDung = db.NguoiDung.Count();
                ViewBag.SoDonHang = db.DonHang.Count();
                ViewBag.DoanhThu = db.DonHang.Sum(x => (decimal?)x.TongTien) ?? 0;

                return View();
            }
        }
        public ActionResult Thongke()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

            // === Tổng quan chính (KPI Cards) ===
            var doanhThu = db.ChiTietDonHangs.Sum(od => (decimal?)(od.Gia * od.SoLuong)) ?? 0;
            var soLuongDonHang = db.DonHangs.Count();
            var soLuongKhachHang = db.NguoiDungs.Count(u => u.MaVaiTro == 2); // Khách hàng
            var soLuongNhanVien = db.NguoiDungs.Count(u => u.MaVaiTro == 3 || u.MaVaiTro == 4); // Nhân viên + Quản lý
            var soLuongSanPham = db.SanPhams.Count();
            var soLuongDanhMuc = db.DanhMucs.Count();
            var tongDanhGia = db.DanhGias.Count();
            var tongTonKho = db.ChiTietSanPhams.Sum(ct => (int?)ct.SoLuongTon) ?? 0;

            // Giá trị tồn kho (Stock Value)
            var giaTriTonKho = db.ChiTietSanPhams
                .Include(ct => ct.SanPham)
                .Sum(ct => (decimal?)(ct.SoLuongTon * ct.SanPham.Gia)) ?? 0;

            var donHangHomNay = db.DonHangs.Count(o => o.NgayDat >= today && o.NgayDat < tomorrow);
            var sanPhamHetHang = db.ChiTietSanPhams.Count(ct => ct.SoLuongTon == 0);
            var sanPhamSapHet = db.ChiTietSanPhams.Count(ct => ct.SoLuongTon > 0 && ct.SoLuongTon <= 5);

            var khachHangMoiThangNay = db.NguoiDungs
                .Count(u => u.MaVaiTro == 2 && u.NgayTao >= firstDayOfMonth);

            var donHangHuy = db.DonHangs.Count(o => o.TrangThai == "Đã hủy");
            var donHangHoanThanh = db.DonHangs.Count(o => o.TrangThai == "Hoàn thành");
            var tyLeHoanThanh = soLuongDonHang > 0
                ? Math.Round((decimal)donHangHoanThanh * 100 / soLuongDonHang, 1) : 0;

            var trungBinhDanhGia = tongDanhGia > 0
                ? Math.Round(db.DanhGias.Average(r => (decimal)r.SoSao), 1) : 0;

            var averageOrderValue = soLuongDonHang > 0
                ? Math.Round(doanhThu / soLuongDonHang, 0) : 0;

            // Gửi tất cả KPI ra View
            ViewBag.DoanhThu = doanhThu;
            ViewBag.SoLuongDonHang = soLuongDonHang;
            ViewBag.SoLuongKhachHang = soLuongKhachHang;
            ViewBag.SoLuongNhanVien = soLuongNhanVien;
            ViewBag.SoLuongSanPham = soLuongSanPham;
            ViewBag.SoLuongDanhMuc = soLuongDanhMuc;
            ViewBag.TongDanhGia = tongDanhGia;
            ViewBag.TongTonKho = tongTonKho;
            ViewBag.GiaTriTonKho = giaTriTonKho;
            ViewBag.DonHangHomNay = donHangHomNay;
            ViewBag.SanPhamHetHang = sanPhamHetHang;
            ViewBag.SanPhamSapHet = sanPhamSapHet;
            ViewBag.KhachHangMoi = khachHangMoiThangNay;
            ViewBag.DonHangHuy = donHangHuy;
            ViewBag.TyLeHoanThanh = tyLeHoanThanh;
            ViewBag.TrungBinhDanhGia = trungBinhDanhGia;
            ViewBag.AverageOrderValue = averageOrderValue;

            // === Doanh thu theo tháng ===
            var doanhThuTheoThangRaw = db.DonHangs
                .Where(o => o.NgayDat.HasValue)
                .SelectMany(o => o.ChiTietDonHangs.Select(od => new
                {
                    Thang = o.NgayDat.Value.Month,
                    Nam = o.NgayDat.Value.Year,
                    Tien = (decimal)(od.Gia * od.SoLuong)
                }))
                .GroupBy(x => new { x.Nam, x.Thang })
                .Select(g => new
                {
                    Nam = g.Key.Nam,
                    Thang = g.Key.Thang,
                    DoanhThu = g.Sum(x => x.Tien)
                })
                .OrderByDescending(x => x.Nam).ThenByDescending(x => x.Thang) // Mới nhất lên đầu
                .ToList();

            ViewBag.DoanhThuTheoThang = doanhThuTheoThangRaw.Select(x =>
            {
                dynamic d = new ExpandoObject();
                d.ThangNam = $"{x.Thang}/{x.Nam}";
                d.DoanhThu = x.DoanhThu;
                return d;
            }).ToList();

            // === Tỷ lệ đơn hàng theo trạng thái ===
            var tyLeDonHang = db.DonHangs
                .GroupBy(o => o.TrangThai ?? "Không xác định")
                .Select(g => new
                {
                    TrangThai = g.Key,
                    SoLuong = g.Count()
                })
                .ToList();

            ViewBag.TyLeDonHang = tyLeDonHang.Select(x =>
            {
                dynamic d = new ExpandoObject();
                d.TrangThai = x.TrangThai;
                d.SoLuong = x.SoLuong;
                return d;
            }).ToList();

            // === Top 5 Sản phẩm bán chạy (group theo MaSanPham) ===
            var topSanPham = db.ChiTietDonHangs
                .Include(cd => cd.ChiTietSanPham.SanPham)
                .GroupBy(cd => cd.ChiTietSanPham.MaSanPham)
                .Select(g => new
                {
                    MaSanPham = g.Key,
                    SoLuongBan = g.Sum(x => x.SoLuong)
                })
                .OrderByDescending(x => x.SoLuongBan)
                .Take(5)
                .ToList();

            var listTopSanPham = topSanPham.Select(item =>
            {
                dynamic obj = new ExpandoObject();
                obj.ProductName = item.MaSanPham != null
                    ? db.SanPhams.FirstOrDefault(p => p.MaSanPham == item.MaSanPham)?.TenSanPham ?? "Không rõ"
                    : "Không rõ";
                obj.Quantity = item.SoLuongBan;
                return obj;
            }).ToList();

            ViewBag.TopSanPhamBanChay = listTopSanPham;

            // === Top 5 Khách hàng mua nhiều nhất ===
            var topKhachHang = db.DonHangs
                .GroupBy(o => o.MaNguoiDung)
                .Select(g => new { MaNguoiDung = g.Key, SoDonHang = g.Count() })
                .OrderByDescending(x => x.SoDonHang)
                .Take(5)
                .ToList();

            var listTopKhachHang = topKhachHang.Select(item =>
            {
                dynamic obj = new ExpandoObject();
                var user = db.NguoiDungs.FirstOrDefault(u => u.MaNguoiDung == item.MaNguoiDung);
                obj.FullName = user?.Ten ?? "Không rõ";
                obj.SoDonHang = item.SoDonHang;
                return obj;
            }).ToList();

            ViewBag.TopKhachHang = listTopKhachHang;

            // === Sản phẩm đánh giá thấp (1-2 sao) ===
            var sanPhamThap = db.DanhGias
                .Where(r => r.SoSao >= 1 && r.SoSao <= 2)
                .GroupBy(r => r.MaSanPham)
                .Select(g => new { MaSanPham = g.Key, SoLuong = g.Count() })
                .OrderByDescending(g => g.SoLuong)
                .Take(5)
                .ToList();

            ViewBag.SanPhamDanhGiaThap = sanPhamThap.Select(item =>
            {
                dynamic obj = new ExpandoObject();
                var sp = db.SanPhams.FirstOrDefault(p => p.MaSanPham == item.MaSanPham);
                obj.ProductName = sp?.TenSanPham ?? "Không rõ";
                obj.SoDanhGiaThap = item.SoLuong;
                return obj;
            }).ToList();

            // === Sản phẩm chưa từng bán ===
            var sanPhamChuaBanRaw = db.SanPhams
                .Where(p => !db.ChiTietDonHangs.Any(cd =>
                    db.ChiTietSanPhams.Any(ct => ct.MaChiTiet == cd.MaChiTietSanPham && ct.MaSanPham == p.MaSanPham)))
                .Select(p => p.TenSanPham)
                .ToList();

            ViewBag.SanPhamChuaBan = sanPhamChuaBanRaw.Select(name =>
            {
                dynamic obj = new ExpandoObject();
                obj.ProductName = name;
                return obj;
            }).ToList();

            return View();
        }
        public ActionResult QuanLySanPham(string search, int page = 1, int pageSize = 10)
        {

            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = 10;

                var query = db.SanPham
                    .Include(s => s.DanhMuc)
                    .Include(s => s.ChiTietSanPham)
                    .Include(s => s.ChiTietSanPham.Select(ct => ct.ThuongHieu))
                    .Include(s => s.ChiTietSanPham.Select(ct => ct.ChatLieu))
                    .Include(s => s.ChiTietSanPham.Select(ct => ct.MauSac))
                    .AsQueryable();
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower().Trim();

                    query = query.Where(s =>
                        s.TenSanPham.ToLower().Contains(search) ||
                        (s.DanhMuc != null && s.DanhMuc.TenDanhMuc.ToLower().Contains(search)) 
                    );
                }
                int totalProducts = query.Count();

                int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
                var result = query
                    .OrderByDescending(s => s.NoiBat)
                    .ThenByDescending(s => s.NgayTao)
                    .ThenByDescending(s => s.MaSanPham)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                ViewBag.TotalPages = totalPages;
                ViewBag.CurrentPage = page;
                ViewBag.Search = search;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalProducts = totalProducts;
                ViewBag.DanhMucList = new SelectList(db.DanhMuc.ToList(), "MaDanhMuc", "TenDanhMuc");

                return View(result);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi tải danh sách sản phẩm: " + ex.Message;
                if (ex.InnerException != null)
                    TempData["ErrorMessage"] += " - Chi tiết: " + ex.InnerException.Message;
                ViewBag.TotalPages = 0;
                ViewBag.CurrentPage = 1;
                ViewBag.TotalProducts = 0;

                return View(new List<SanPham>());
            }
        }
        public ActionResult ThemSanPham()
        {
            ViewBag.DanhMucList = new SelectList(db.DanhMuc.ToList(), "MaDanhMuc", "TenDanhMuc");
            ViewBag.ThuongHieuList = new SelectList(db.ThuongHieu.ToList(), "MaThuongHieu", "TenThuongHieu");
            ViewBag.ChatLieuList = new SelectList(db.ChatLieu.ToList(), "MaChatLieu", "TenChatLieu");
            ViewBag.MauSacList = new SelectList(db.MauSac.ToList(), "MaMau", "TenMau");

            return View(new SanPham());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemSanPham(
    SanPham model,
    HttpPostedFileBase fileAnhChinh,
    HttpPostedFileBase fileAnhPhu1,
    HttpPostedFileBase fileAnhPhu2,
    HttpPostedFileBase fileAnhPhu3,
    int MaThuongHieu,
    int MaChatLieu,
    int MaMau,
    int SoLuongTon = 10)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    model.AnhChinh = SaveImage(fileAnhChinh);
                    model.AnhPhu1 = SaveImage(fileAnhPhu1);
                    model.AnhPhu2 = SaveImage(fileAnhPhu2);
                    model.AnhPhu3 = SaveImage(fileAnhPhu3);

                    db.SanPham.Add(model);
                    db.SaveChanges();

                    var chiTiet = new ChiTietSanPham
                    {
                        MaSanPham = model.MaSanPham,
                        MaThuongHieu = MaThuongHieu,
                        MaChatLieu = MaChatLieu,
                        MaMau = MaMau,
                        SoLuongTon = SoLuongTon > 0 ? SoLuongTon : 10
                    };

                    db.ChiTietSanPham.Add(chiTiet);
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
            ViewBag.DanhMucList = new SelectList(db.DanhMuc.ToList(), "MaDanhMuc", "TenDanhMuc");
            ViewBag.ThuongHieuList = new SelectList(db.ThuongHieu.ToList(), "MaThuongHieu", "TenThuongHieu");
            ViewBag.ChatLieuList = new SelectList(db.ChatLieu.ToList(), "MaChatLieu", "TenChatLieu");
            ViewBag.MauSacList = new SelectList(db.MauSac.ToList(), "MaMau", "TenMau");

            return View(model);
        }
        private string SaveImage(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
                return null;

            string extension = Path.GetExtension(file.FileName);
            string fileName = Guid.NewGuid() + extension;

            string folder = Server.MapPath("~/AnhWeb");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string path = Path.Combine(folder, fileName);

            file.SaveAs(path);

            return "/AnhWeb/" + fileName;
        }
        public ActionResult ChinhSuaSanPham(int id)
        {
            try
            {
                var sanPham = db.SanPham
                    .Include(s => s.DanhMuc)
                    .FirstOrDefault(s => s.MaSanPham == id);

                if (sanPham == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                    return RedirectToAction("QuanLySanPham");
                }
                ViewBag.DanhMucList = new SelectList(db.DanhMuc.ToList(), "MaDanhMuc", "TenDanhMuc", sanPham.MaDanhMuc);
                ViewBag.ThuongHieuList = new SelectList(db.ThuongHieu.ToList(), "MaThuongHieu", "TenThuongHieu");
                ViewBag.ChatLieuList = new SelectList(db.ChatLieu.ToList(), "MaChatLieu", "TenChatLieu");
                ViewBag.MauSacList = new SelectList(db.MauSac.ToList(), "MaMau", "TenMau");
                var chiTiet = db.ChiTietSanPham
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
                    var sanPham = db.SanPham.Find(model.MaSanPham);
                    if (sanPham == null)
                    {
                        ViewBag.Error = "Không tìm thấy sản phẩm để chỉnh sửa.";
                        return View(model);
                    }
                    sanPham.TenSanPham = model.TenSanPham;
                    sanPham.Gia = model.Gia;
                    sanPham.MoTa = model.MoTa;
                    sanPham.AnhChinh = model.AnhChinh;
                    sanPham.AnhPhu1 = model.AnhPhu1;
                    sanPham.AnhPhu2 = model.AnhPhu2;
                    sanPham.AnhPhu3 = model.AnhPhu3;
                    sanPham.MaDanhMuc = model.MaDanhMuc;
                    sanPham.NoiBat = model.NoiBat;
                    var chiTiet = db.ChiTietSanPham.FirstOrDefault(ct => ct.MaSanPham == model.MaSanPham);

                    if (chiTiet != null)
                    {
                        if (MaThuongHieu.HasValue) chiTiet.MaThuongHieu = MaThuongHieu.Value;
                        if (MaChatLieu.HasValue) chiTiet.MaChatLieu = MaChatLieu.Value;
                        if (MaMau.HasValue) chiTiet.MaMau = MaMau.Value;
                        if (SoLuongTon.HasValue) chiTiet.SoLuongTon = SoLuongTon.Value;
                    }
                    else
                    {
                        chiTiet = new ChiTietSanPham
                        {
                            MaSanPham = model.MaSanPham,
                            MaThuongHieu = MaThuongHieu ?? 1,
                            MaChatLieu = MaChatLieu ?? 1,
                            MaMau = MaMau ?? 1,
                            SoLuongTon = SoLuongTon ?? 10
                        };
                        db.ChiTietSanPham.Add(chiTiet);
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
            ViewBag.DanhMucList = new SelectList(db.DanhMuc.ToList(), "MaDanhMuc", "TenDanhMuc", model.MaDanhMuc);
            ViewBag.ThuongHieuList = new SelectList(db.ThuongHieu.ToList(), "MaThuongHieu", "TenThuongHieu", MaThuongHieu);
            ViewBag.ChatLieuList = new SelectList(db.ChatLieu.ToList(), "MaChatLieu", "TenChatLieu", MaChatLieu);
            ViewBag.MauSacList = new SelectList(db.MauSac.ToList(), "MaMau", "TenMau", MaMau);

            return View(model);
        }
        [HttpPost]

        public ActionResult XoaSanPham(int id)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var sanPham = db.SanPham.Find(id);
                    if (sanPham == null)
                    {
                        TempData["Message"] = "Không tìm thấy sản phẩm để xóa!";
                        return RedirectToAction("QuanLySanPham");
                    }
                    var chiTietGioHangs = db.ChiTietGioHang
                        .Where(cgh => db.ChiTietSanPham
                            .Any(ct => ct.MaChiTiet == cgh.MaChiTietSanPham && ct.MaSanPham == id))
                        .ToList();

                    if (chiTietGioHangs.Any())
                        db.ChiTietGioHang.RemoveRange(chiTietGioHangs);
                    var chiTietDonHangs = db.ChiTietDonHang
                        .Where(cdh => db.ChiTietSanPham
                            .Any(ct => ct.MaChiTiet == cdh.MaChiTietSanPham && ct.MaSanPham == id))
                        .ToList();

                    if (chiTietDonHangs.Any())
                        db.ChiTietDonHang.RemoveRange(chiTietDonHangs);

                    var danhGias = db.DanhGia.Where(d => d.MaSanPham == id).ToList();
                    if (danhGias.Any())
                        db.DanhGia.RemoveRange(danhGias);

                    var chiTietSanPhams = db.ChiTietSanPham.Where(ct => ct.MaSanPham == id).ToList();
                    if (chiTietSanPhams.Any())
                        db.ChiTietSanPham.RemoveRange(chiTietSanPhams);

                    db.SanPham.Remove(sanPham);

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
        public ActionResult QuanLyBanner(string search = "", int page = 1, int pageSize = 10)
        {
            var query = db.Banner.AsQueryable();

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
        public ActionResult ThemBanner()
        {
            Banner model = new Banner
            {
                ThuTu = 1,
                TrangThai = true
            };

            return View(model);
        }
        public ActionResult ChinhSuaBanner(int id)
        {
            var banner = db.Banner.Find(id);
            if (banner == null) return HttpNotFound();

            return View(banner);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChinhSuaBanner(Banner model, HttpPostedFileBase fileUpload)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var banner = db.Banner.Find(model.MaBanner);
                    if (banner == null) return HttpNotFound();

                    banner.TieuDe = model.TieuDe;
                    banner.Link = model.Link;
                    banner.ViTri = model.ViTri;
                    banner.TrangThai = model.TrangThai;
                    banner.ThuTu = model.ThuTu;
                    banner.NgayBatDau = model.NgayBatDau;
                    banner.NgayKetThuc = model.NgayKetThuc;

                    // 🔥 XỬ LÝ ẢNH
                    if (fileUpload != null && fileUpload.ContentLength > 0)
                    {
                        string extension = Path.GetExtension(fileUpload.FileName);
                        string fileName = Guid.NewGuid() + extension;

                        string path = Path.Combine(Server.MapPath("~/AnhWeb"), fileName);
                        fileUpload.SaveAs(path);

                        banner.HinhAnh = "/AnhWeb/" + fileName;
                    }

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
                    var banner = db.Banner.Find(id);

                    if (banner == null)
                    {
                        TempData["Message"] = "Không tìm thấy banner!";
                        TempData["MessageType"] = "warning";
                        return RedirectToAction("QuanLyBanner");
                    }

                    db.Banner.Remove(banner);
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
            if (Session["VaiTro"] == null || Convert.ToInt32(Session["VaiTro"]) != 1)
            {
                TempData["Message"] = "Bạn cần quyền Admin để truy cập trang này!";
                return RedirectToAction("Login", "Admin");
            }

            var users = db.NguoiDung.Include(u => u.VaiTro).AsQueryable();

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
        public ActionResult ThemTaiKhoan()
        {
            if (Session["VaiTro"] == null || Convert.ToInt32(Session["VaiTro"]) != 1)
            {
                TempData["Message"] = "Bạn cần quyền Admin!";
                return RedirectToAction("Login", "Admin");
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
            return View(new NguoiDung());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemTaiKhoan(NguoiDung model, string MatKhau)
        {
            if (Session["VaiTro"] == null || Convert.ToInt32(Session["VaiTro"]) != 1)
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

            if (db.NguoiDung.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email đã tồn tại!");
                return View(model);
            }

            try
            {
                model.MatKhau = MatKhau; 
                model.NgayTao = DateTime.Now;

                db.NguoiDung.Add(model);
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
        public ActionResult ChinhSuaTaiKhoan(int? id)
        {
            if (Session["VaiTro"] == null || Convert.ToInt32(Session["VaiTro"]) != 1)
                return RedirectToAction("Login", "Admin");

            if (id == null) return HttpNotFound();

            var user = db.NguoiDung.Find(id);
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
            if (Session["VaiTro"] == null || Convert.ToInt32(Session["VaiTro"]) != 1)
                return RedirectToAction("Login", "Admin");

            if (ModelState.IsValid)
            {
                var user = db.NguoiDung.Find(model.MaNguoiDung);
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

        [HttpPost]

        public ActionResult XoaTaiKhoan(int id)
        {
            if (Session["VaiTro"] == null || Convert.ToInt32(Session["VaiTro"]) != 1)
            {
                TempData["Message"] = "Bạn cần quyền Admin!";
                return RedirectToAction("Login", "Admin");
            }

            var user = db.NguoiDung
                .Include(u => u.GioHang)
                .Include(u => u.DonHang)
                .Include(u => u.DanhGia)
                .FirstOrDefault(u => u.MaNguoiDung == id);

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản!";
                return RedirectToAction("DanhSachTaiKhoan");
            }

            try
            {
                if (user.GioHang != null) db.GioHang.RemoveRange(user.GioHang);
                if (user.DonHang != null) db.DonHang.RemoveRange(user.DonHang);
                if (user.DanhGia != null) db.DanhGia.RemoveRange(user.DanhGia);

                db.NguoiDung.Remove(user);
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemBanner(Banner model, HttpPostedFileBase fileUpload)
        {

            if (ModelState.IsValid)
            {
                try
                {
                    if (fileUpload != null && fileUpload.ContentLength > 0)
                    {
                        string extension = Path.GetExtension(fileUpload.FileName);
                        string fileName = Guid.NewGuid() + extension;

                        string folder = Server.MapPath("~/AnhWeb");

                        if (!Directory.Exists(folder))
                        {
                            Directory.CreateDirectory(folder);
                        }

                        string path = Path.Combine(folder, fileName);
                        fileUpload.SaveAs(path);

                        model.HinhAnh = "/AnhWeb/" + fileName;
                    }

                    db.Banner.Add(model);
                    db.SaveChanges();

                    TempData["Message"] = "Thêm banner thành công!";
                    return RedirectToAction("QuanLyBanner");
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Lỗi: " + ex.Message;
                }
            }

            return View(model);
        }
    }
}