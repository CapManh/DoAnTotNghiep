namespace DoAnTotNghiep.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Banner")]
    public partial class Banner
    {
        [Key]
        public int MaBanner { get; set; }

        [StringLength(200)]
        public string TieuDe { get; set; }

        [StringLength(255)]
        public string HinhAnh { get; set; }

        [StringLength(255)]
        public string Link { get; set; }

        [StringLength(50)]
        public string ViTri { get; set; }

        public bool? TrangThai { get; set; }

        public int? ThuTu { get; set; }

        public DateTime? NgayBatDau { get; set; }

        public DateTime? NgayKetThuc { get; set; }
    }
}
