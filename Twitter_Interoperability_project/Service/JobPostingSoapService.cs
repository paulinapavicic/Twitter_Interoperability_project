using System.Xml.Linq;
using System.Xml.XPath;
using Twitter_Interoperability_project.Interfaces;

namespace Twitter_Interoperability_project.Service
{
    public class JobPostingSoapService: IJobPostingSoapService
    {
        private const string XmlFilePath = "App_Data/jobpostings.xml";

        public string SearchJobPostings(string term)
        {
            if (!File.Exists(XmlFilePath))
                throw new FileNotFoundException("JobPosting data not found. Generate XML first.");

            var doc = XDocument.Load(XmlFilePath);
            var searchTerm = (term ?? "").Trim().ToLower();

            // Search all fields in each JobPosting
            var matches = doc.Root.Elements("JobPosting")
                .Where(job =>
                    job.Elements().Any(field =>
                        (field.Value ?? "").ToLower().Contains(searchTerm)
                    )
                );

            // Optionally: Log matches for debugging
            File.WriteAllText("App_Data/soap_matched_debug.xml", new XElement("Debug", matches).ToString());

            return new XElement("SearchResults", matches).ToString();
        
    }

    }
}
