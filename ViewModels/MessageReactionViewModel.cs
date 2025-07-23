namespace Zela.ViewModels;

public class MessageReactionViewModel
{
    public long ReactionId { get; set; }
    public long MessageId { get; set; }             // FK -> Message
    public int UserId { get; set; }                 // FK -> User
    public string UserName { get; set; }            // Like, Love, Haha, Wow, Sad, Angry
    public string ReactionType { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MessageReactionSummaryViewModel
{
    public string ReactionType { get; set; }
    public int Count { get; set; }
    public List<string> UserNames { get; set; } = new List<string>();
    public bool HasUserReaction { get; set; }
}