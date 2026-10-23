using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS.Models.Entities;

public class QuyTrinhPheDuyet
{
    public int ID { get; set; }
    [Required,MaxLength(50)] public string LoaiYeuCau { get; set; }=string.Empty;
    [Required,MaxLength(150)] public string TenQuyTrinh { get; set; }=string.Empty;
    public bool HoatDong { get; set; }=true;
    public ICollection<BuocPheDuyet> CacBuoc { get; set; }=new List<BuocPheDuyet>();
}

public class BuocPheDuyet
{
    public int ID { get; set; }
    public int QuyTrinhPheDuyetID { get; set; }
    public int ThuTu { get; set; }
    [Required,MaxLength(50)] public string VaiTroDuyet { get; set; }="QuanLy";
    [MaxLength(200)] public string? TenBuoc { get; set; }
    [ForeignKey(nameof(QuyTrinhPheDuyetID))] public QuyTrinhPheDuyet? QuyTrinh { get; set; }
}
