using System.ComponentModel.DataAnnotations;

namespace Twitter_Interoperability_project.Models
{
    public class JobPosting
    {

        public string Id { get; set; }


        public string RestId { get; set; }
       
        public string Title { get; set; }
        public string ExternalUrl { get; set; }
        public string JobDescription { get; set; }
        public string JobPageUrl { get; set; }
        public string Location { get; set; }
        public string LocationType { get; set; }
        public string SeniorityLevel { get; set; }
        public string Team { get; set; }
        
        public string CompanyName { get; set; }
        public string CompanyLogoUrl { get; set; }
    }
}
