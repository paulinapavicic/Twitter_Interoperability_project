namespace Twitter_Interoperability_project.Models
{
    public class RngValidationViewModel
    {
        public string XmlData { get; set; }
        public string RngData { get; set; }
        public string Result { get; set; }

        public List<JobPosting> JobPostings { get; set; } = new List<JobPosting>();
    }
}
