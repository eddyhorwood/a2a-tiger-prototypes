using System.Collections.Generic;

namespace XeroDotnetSampleApp.Clients
{

    /// <summary>
    /// We dont use this at the moment but it might be useful later for logging so I included the DTO
    /// This shape is based on Akahu's APIIssue, IssueResponse format 
    /// See https://xero.slack.com/archives/C09L46K63R7/p1760907415388419
    /// </summary>
    public class AkahuProblemDetails
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<AkahuIssue> Issues { get; set; }
    }

    public class AkahuIssue
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public List<object> Path { get; set; }
    }
}