using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using Twitter_Interoperability_project.Interfaces;

namespace Twitter_Interoperability_project.Service
{
    public class JobPostingSoapService: IJobPostingSoapService
    {
        private const string XmlFilePath = "App_Data/jobpostings.xml";

        // Helper to normalize whitespace and case
        private string NormalizeSpace(string input)
        {
            if (input == null) return "";
            return Regex.Replace(input.Trim(), @"\s+", " ");
        }

        public string SearchJobPostings(string term)
        {
            if (!File.Exists(XmlFilePath))
                throw new FileNotFoundException("JobPosting data not found. Generate XML first.");

            XDocument doc;
            try
            {
                doc = XDocument.Load(XmlFilePath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to load or parse job postings XML: " + ex.Message, ex);
            }

            var searchTerm = NormalizeSpace(term).ToLower();

           
            var matches = doc.Root.Elements("JobPosting")
                .Where(job =>
                    job.Elements().Any(field =>
                        NormalizeSpace((field.Value ?? "")).ToLower().Contains(searchTerm)
                    )
                )
                .ToList();

            
            try
            {
                Directory.CreateDirectory("App_Data");
                File.WriteAllText("App_Data/soap_matched_debug.xml", new XElement("Debug", matches).ToString());
            }
            catch
            {
                
            }

           
            return new XElement("SearchResults", matches).ToString();
        }
    }

    
}
