using BanSach.Components.Model;

public class Review
{
    public int ReviewId { get; set; }
    public int UserId { get; set; }        // ID người dùng đã đánh giá
    public int BillId { get; set; }        // ID đơn hàng mà người dùng đánh giá
    public int Rating { get; set; }        // Điểm đánh giá (ví dụ: 1 - 5)
    public string ReviewText { get; set; } // Nội dung đánh giá
    public DateTime CreatedAt { get; set; } // Thời gian tạo đánh giá
}
