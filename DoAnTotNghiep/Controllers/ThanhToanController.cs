using DoAnTotNghiep.Models;
using QRCoder;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
public class ThanhToanController : Controller
{
    Model1 db = new Model1();

    public ActionResult QR()
    {
        dynamic data = Session["Checkout"];

        if (data == null)
            return RedirectToAction("GioHang", "Home");

        decimal tongTien = data.TongTien;

        decimal tienCoc = tongTien * 0.1m;
        long tienCocInt = (long)tienCoc;

        string maDonHang = DateTime.Now.Ticks.ToString().Substring(10);

        string stk = "123456789";
        string bank = "VCB";
        string ten = "LE TUAN MANH";

        string noiDung = "DH" + maDonHang;

        string qr =
            $"https://img.vietqr.io/image/{bank}-{stk}-compact2.png" +
            $"?amount={tienCocInt}" +
            $"&addInfo={noiDung}" +
            $"&accountName={ten}";

        ViewBag.QR = qr;
        ViewBag.TienCoc = tienCocInt;
        ViewBag.MaDonHang = maDonHang;
        ViewBag.NoiDung = noiDung;

        return View();
    }
    public ActionResult QR1()
    {
        dynamic data = Session["Checkout"];

        if (data == null)
            return RedirectToAction("GioHang", "Home");

        decimal tongTien = data.TongTien;
        long tienCocInt = (long)(tongTien);

        string maDonHang = DateTime.Now.Ticks.ToString().Substring(10);

        string stk = "123456789";
        string bank = "VCB";
        string ten = "LE TUAN MANH";

        string noiDung = "DH" + maDonHang;

        string qr =
            $"https://img.vietqr.io/image/{bank}-{stk}-compact2.png" +
            $"?amount={tienCocInt}" +
            $"&addInfo={noiDung}" +
            $"&accountName={ten}";

        ViewBag.QR1 = qr;
        ViewBag.TienCoc = tienCocInt;
        ViewBag.MaDonHang = maDonHang;
        ViewBag.NoiDung1 = noiDung;

        return View();
    }
    [HttpPost]
    public JsonResult XacNhanChuyenKhoan()
    {
        var checkout = Session["Checkout"];

        if (checkout == null)
            return Json(new { success = false });

        int maND = (int)Session["MaNguoiDung"];

        var cart = db.GioHang
            .Include(x => x.SanPham)
            .Where(x => x.MaNguoiDung == maND)
            .ToList();

        if (!cart.Any())
            return Json(new { success = false });

        decimal tongTien =
            cart.Sum(x => (x.SanPham.Gia ?? 0) * (x.SoLuong ?? 0));

        decimal phiShip = 39000;
        decimal tongThanhToan = tongTien + phiShip;
        decimal tienCoc = tongThanhToan * 0.1m;
        var donHang = new DonHang
        {
            MaNguoiDung = maND,
            TongTien = tongThanhToan,
            TrangThai = "Chờ xác nhận",
            NgayDat = DateTime.Now
        };

        db.DonHang.Add(donHang);
        db.SaveChanges();
        foreach (var item in cart)
        {
            db.ChiTietDonHang.Add(new ChiTietDonHang
            {
                MaDonHang = donHang.MaDonHang,
                MaChiTietSanPham = item.MaSanPham,
                SoLuong = item.SoLuong ?? 0,
                Gia = item.SanPham.Gia
            });
        }
        db.ThanhToan.Add(new ThanhToan
        {
            MaDonHang = donHang.MaDonHang,
            MaPhuongThuc = 1,
            SoTien = tienCoc,
            TrangThai = "Chờ xác nhận",
            MaGiaoDich = Guid.NewGuid().ToString(),
            NgayThanhToan = DateTime.Now
        });
        db.GioHang.RemoveRange(cart);

        db.SaveChanges();

        Session.Remove("Checkout");

        return Json(new { success = true });
    }
}

