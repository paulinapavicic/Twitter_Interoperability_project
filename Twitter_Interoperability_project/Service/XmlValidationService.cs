using System.Xml;

namespace Twitter_Interoperability_project.Service
{
    public class XmlValidationService
    {
        public List<string> ValidateXmlWithXsdString(string xmlContent, string xsdContent)
        {
            var errors = new List<string>();
            var settings = new XmlReaderSettings();
            using (var schemaReader = new StringReader(xsdContent))
            {
                settings.Schemas.Add(null, XmlReader.Create(schemaReader));
            }
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationEventHandler += (sender, e) => errors.Add(e.Message);

            using (var reader = XmlReader.Create(new StringReader(xmlContent), settings))
            {
                try
                {
                    while (reader.Read()) { }
                }
                catch (XmlException ex)
                {
                    errors.Add(ex.Message);
                }
            }
            return errors;
        }
    }
}
