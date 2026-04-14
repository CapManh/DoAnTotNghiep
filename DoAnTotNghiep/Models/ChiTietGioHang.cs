namespace DoAnTotNghiep.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("ChiTietGioHang")]
    public partial class ChiTietGioHang
    {
        [Key]
        public int MaChiTiet { get; set; }

        public int? MaGioHang { get; set; }

        public int? MaChiTietSanPham { get; set; }

        public int? SoLuong { get; set; }

        public virtual ChiTietSanPham ChiTietSanPham { get; set; }

        public virtual GioHang GioHang { get; set; }
    }
}
