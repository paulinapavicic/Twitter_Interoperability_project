using System.Collections.Generic;
using System.IO;
using Commons.Xml.Relaxng;

namespace Twitter_Interoperability_project.Service
{
    public class RngValidationService
    {
        public List<string> ValidateXmlWithRngString(string xmlContent, string rngContent)
        {
            var errors = new List<string>();
            try
            {
                using (var schemaReader = new StringReader(rngContent))
                using (var xmlSchemaReader = System.Xml.XmlReader.Create(schemaReader))
                using (var xmlReader = new StringReader(xmlContent))
                {
                    var pattern = RelaxngPattern.Read(xmlSchemaReader);
                    var validator = new RelaxngValidatingReader(System.Xml.XmlReader.Create(xmlReader), pattern);

                    while (validator.Read()) { }
                }
            }
            catch (RelaxngException ex)
            {
                errors.Add("Relax NG validation error: " + ex.Message);
            }
            catch (System.Xml.XmlException ex)
            {
                errors.Add("XML error: " + ex.Message);
            }
            return errors;
        }

    }
}
