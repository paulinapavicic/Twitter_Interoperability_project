using Microsoft.AspNetCore.Mvc;
using Minio.DataModel.Args;
using Minio;
using System.Text;
using System.Xml.Serialization;
using Twitter_Interoperability_project.Models;
using Twitter_Interoperability_project.Service;

namespace Twitter_Interoperability_project.Controllers
{
    public class RngController : Controller
    {
        private static List<JobPosting> _jobPostings = new List<JobPosting>();
        private readonly RngValidationService _validator;
        private readonly IMinioClient _minio;

        public RngController(IConfiguration configuration)
        {
            _validator = new RngValidationService();

            _minio = new MinioClient()
                .WithEndpoint(configuration["MinIO:Endpoint"])
                .WithCredentials(
                    configuration["MinIO:AccessKey"],
                    configuration["MinIO:SecretKey"])
             
                .Build();
        }

        [HttpGet]
        public IActionResult Index()
        {
            var model = new RngValidationViewModel();
            ViewBag.JobPostings = _jobPostings;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(RngValidationViewModel model, string submit)
        {
            if (submit == "Sample")
            {
                model.XmlData = SampleXml();
                model.RngData = SampleRng();
                model.Result = null;
            }
            else // Validate and save to MinIO if valid
            {
                var errors = _validator.ValidateXmlWithRngString(model.XmlData, model.RngData);

                if (errors.Count == 0)
                {
                    try
                    {
                        // Save to MinIO
                        var bucketName = "interoperability";
                        var objectName = $"jobpostings-rng/{Guid.NewGuid()}.xml";
                        var xmlBytes = Encoding.UTF8.GetBytes(model.XmlData);

                        using var stream = new MemoryStream(xmlBytes);

                        await _minio.PutObjectAsync(
                            new PutObjectArgs()
                                .WithBucket(bucketName)
                                .WithObject(objectName)
                                .WithStreamData(stream)
                                .WithObjectSize(stream.Length)
                                .WithContentType("application/xml"));

                        // Deserialize and store locally
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

        private string SampleRng() => @"<element name='JobPosting' xmlns='http://relaxng.org/ns/structure/1.0'>
  <element name='Id'><text/></element>
  <element name='RestId'><text/></element>
  <element name='Title'><text/></element>
  <element name='ExternalUrl'><text/></element>
  <element name='JobDescription'><text/></element>
  <element name='JobPageUrl'><text/></element>
  <element name='Location'><text/></element>
  <element name='LocationType'><text/></element>
  <element name='SeniorityLevel'><text/></element>
  <element name='Team'><text/></element>
  <element name='CompanyName'><text/></element>
  <element name='CompanyLogoUrl'><text/></element>
</element>";
    }
}

