namespace XeroDotnetSampleApp.Config
{
    public class AkahuSettings
    {
        public const string SectionName = "AkahuSettings";
        
        public string BaseUrl { get; set; } = string.Empty;
        public string AppToken { get; set; } = string.Empty;
        public string AppSecret { get; set; } = string.Empty;
    }
}