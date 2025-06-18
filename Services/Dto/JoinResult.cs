namespace Zela.Services.Dto
{
    public class JoinResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }
        public List<string> Peers { get; set; }
    }
}