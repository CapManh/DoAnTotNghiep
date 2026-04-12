namespace DoAnTotNghiep.Moldes
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("DanhGia")]
    public partial class DanhGia
    {
        [Key]
        public int MaDanhGia { get; set; }

        public int? MaNguoiDung { get; set; }

        public int? MaSanPham { get; set; }

        public int? SoSao { get; set; }

        public string NoiDung { get; set; }

        public DateTime? NgayDanhGia { get; set; }

        public virtual NguoiDung NguoiDung { get; set; }

        public virtual SanPham SanPham { get; set; }
    }
}
