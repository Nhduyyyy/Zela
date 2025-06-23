namespace Zela.ViewModels;

public class AddReactionRequest
{
    public long MessageId { get; set; }
    public string ReactionType { get; set; }
}

public class RemoveReactionRequest
{
    public long MessageId { get; set; }
}   