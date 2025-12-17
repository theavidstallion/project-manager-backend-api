// CommentResponseDto.cs
public class CommentResponseDto
{
    public int Id { get; set; }
    public string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string AuthorId { get; set; }
    public string AuthorName { get; set; } // Flattened property

    public int TaskId { get; set; }
}