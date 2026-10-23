namespace QLNS.Models.ViewModels;

public class InternalPortalViewModel
{
    public IReadOnlyList<Entities.TaiLieuNoiBo> Documents { get; set; }=[];
    public IReadOnlyList<Entities.ThongBaoNoiBo> Announcements { get; set; }=[];
    public IReadOnlyList<Entities.KhaoSatNoiBo> Surveys { get; set; }=[];
    public ISet<int> ConfirmedDocumentIds { get; set; }=new HashSet<int>();
    public ISet<int> AnsweredSurveyIds { get; set; }=new HashSet<int>();
    public string? ChatQuestion { get; set; }
    public IReadOnlyList<ChatbotResult> ChatResults { get; set; }=[];
}

public record ChatbotResult(string Title,string Summary,string Type,int Id);
