namespace DoAnTotNghiep.Moldes
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("ThanhToan")]
    public partial class ThanhToan
    {
        [Key]
        public int MaThanhToan { get; set; }

        public int? MaDonHang { get; set; }

        public int? MaPhuongThuc { get; set; }

        public decimal? SoTien { get; set; }

        [StringLength(50)]
        public string TrangThai { get; set; }

        [StringLength(100)]
        public string MaGiaoDich { get; set; }

        public DateTime? NgayThanhToan { get; set; }

        public virtual DonHang DonHang { get; set; }

        public virtual PhuongThucThanhToan PhuongThucThanhToan { get; set; }
    }
}
