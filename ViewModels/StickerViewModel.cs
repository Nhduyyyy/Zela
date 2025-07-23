namespace Zela.ViewModels;

public class StickerViewModel
{
    public long StickerId { get; set; }
    public int RecipientId { get; set; }
    public string StickerName { get; set; }
    public string StickerUrl { get; set; }
    // sticker type là tên thư mục chứa loại sticker đó
    public string StickerType { get; set; }
}