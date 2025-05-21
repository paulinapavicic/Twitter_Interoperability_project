using System.Xml.Linq;
using System.Xml.XPath;
using Twitter_Interoperability_project.Interfaces;

namespace Twitter_Interoperability_project.Service
{
    public class TwitterSoapService: ITwitterSoapService
    {
        private const string XmlFilePath = "App_Data/tweets.xml";

        public string SearchTweets(string term)
        {
            if (!System.IO.File.Exists(XmlFilePath))
                throw new FileNotFoundException("Twitter data not found. Generate XML first.");

            var doc = XDocument.Load(XmlFilePath);
            var escapedTerm = term.Replace("'", "''");

            // Case-insensitive XPath search in Text or Author
            var xpath = $"/Tweets/Tweet[" +
                        $"contains(translate(Text, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), '{escapedTerm.ToLower()}') or " +
                        $"contains(translate(Author, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), '{escapedTerm.ToLower()}')]";

            var matchedTweets = doc.XPathSelectElements(xpath);
            return new XElement("SearchResults", matchedTweets).ToString();
        }

        }
}
