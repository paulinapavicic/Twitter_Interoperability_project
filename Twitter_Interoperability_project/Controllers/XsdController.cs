using Microsoft.AspNetCore.Mvc;
using Minio.DataModel.Args;
using Minio;
using System.Text;
using System.Xml.Serialization;
using Twitter_Interoperability_project.Models;
using Twitter_Interoperability_project.Service;

namespace Twitter_Interoperability_project.Controllers
{
    public class XsdController : Controller
    {
        private static List<JobPosting> _jobPostings = new List<JobPosting>();
        private readonly XmlValidationService _validator;
        private readonly IMinioClient _minio;

        public XsdController(IConfiguration configuration)
        {
            _validator = new XmlValidationService();

            // Initialize MinIO client using configuration
            _minio = new MinioClient()
       .WithEndpoint(configuration["MinIO:Endpoint"])
       .WithCredentials(configuration["MinIO:AccessKey"], configuration["MinIO:SecretKey"])
       .Build();

        }

        [HttpGet]
        public IActionResult Index()
        {
            var model = new XsdValidationViewModel();
            ViewBag.JobPostings = _jobPostings;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(XsdValidationViewModel model, string submit)
        {
            if (submit == "Sample")
            {
                model.XmlData = SampleXml();
                model.XsdData = SampleXsd();
                model.Result = null;
            }
            else 
            {
                var errors = _validator.ValidateXmlWithXsdString(model.XmlData, model.XsdData);

                if (errors.Count == 0)
                {
                    try
                    {
                       
                        var bucketName = "interoperability";
                        var objectName = $"jobpostings/{Guid.NewGuid()}.xml";
                        var xmlBytes = Encoding.UTF8.GetBytes(model.XmlData);

                        using var stream = new MemoryStream(xmlBytes);

                        await _minio.PutObjectAsync(
                            new PutObjectArgs()
                                .WithBucket(bucketName)
                                .WithObject(objectName)
                                .WithStreamData(stream)
                                .WithObjectSize(stream.Length)
                                .WithContentType("application/xml"));

                       
                        var serializer = new XmlSerializer(typeof(JobPosting));
                        using (var reader = new StringReader(model.XmlData))
                        {
                            var job = (JobPosting)serializer.Deserialize(reader);
                            _jobPostings.Add(job);
                        }

                        model.Result = "<span style='color:green;'>XML is valid and saved to MinIO!</span>";
                    }
                    catch (Exception ex)
                    {
                        model.Result = $"<span style='color:red;'>Error saving to MinIO: {ex.Message}</span>";
                    }
                }
                else
                {
                    model.Result = "<span style='color:red;'>Validation errors:<br/>" +
                                  string.Join("<br/>", errors) + "</span>";
                }
            }

            ViewBag.JobPostings = _jobPostings;
            return View(model);
        }


        private string SampleXml() => @"<JobPosting>
  <Id>QXBpSm9iUmVzdWx0czoxNzY2ODE5MDA4MDI2OTMxMjAw</Id>
  <RestId>1766819008026931200</RestId>
  <Title>Senior Python Developer</Title>
  <ExternalUrl>https://jobs.smartrecruiters.com/Devoteam/743999972803753-senior-python-developer</ExternalUrl>
  <JobDescription>Devoteam is a leading consulting firm focused on digital strategy...</JobDescription>
  <JobPageUrl>https://x.com/i/jobs/1766819008026931200</JobPageUrl>
  <Location>Machelen, Vlaams Gewest, be</Location>
  <LocationType>onsite</LocationType>
  <SeniorityLevel>senior</SeniorityLevel>
  <Team>Development</Team>
  <CompanyName>Devoteam</CompanyName>
  <CompanyLogoUrl>https://pbs.twimg.com/profile_images/1082991650794889217/h4Bo8Z5E_normal.jpg</CompanyLogoUrl>
</JobPosting>";


        private string SampleXsd() => @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>
  <xs:element name='JobPosting'>
    <xs:complexType>
      <xs:sequence>
        <xs:element name='Id' type='xs:string'/>
        <xs:element name='RestId' type='xs:string'/>
        <xs:element name='Title' type='xs:string'/>
        <xs:element name='ExternalUrl' type='xs:string'/>
        <xs:element name='JobDescription' type='xs:string'/>
        <xs:element name='JobPageUrl' type='xs:string'/>
        <xs:element name='Location' type='xs:string'/>
        <xs:element name='LocationType' type='xs:string'/>
        <xs:element name='SeniorityLevel' type='xs:string'/>
        <xs:element name='Team' type='xs:string'/>
        <xs:element name='CompanyName' type='xs:string'/>
        <xs:element name='CompanyLogoUrl' type='xs:string'/>
      </xs:sequence>
    </xs:complexType>
  </xs:element>
</xs:schema>";

    }
}

