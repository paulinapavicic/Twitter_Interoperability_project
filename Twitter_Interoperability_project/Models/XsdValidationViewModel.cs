﻿namespace Twitter_Interoperability_project.Models
{
    public class XsdValidationViewModel
    {
        public string XmlData { get; set; }
        public string XsdData { get; set; }
        public string Result { get; set; }
        public List<JobPosting> JobPostings { get; set; } = new List<JobPosting>();
    }
}
