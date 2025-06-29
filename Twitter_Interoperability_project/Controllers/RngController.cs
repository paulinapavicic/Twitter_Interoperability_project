using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.ApiEndpoints;
using Minio.DataModel.Args;
using System.Text;
using System.Xml.Serialization;
using Twitter_Interoperability_project.Models;
using Twitter_Interoperability_project.Service;
using static Twitter_Interoperability_project.Controllers.XsdController;

namespace Twitter_Interoperability_project.Controllers
{
    public class RngController : Controller
    {
        private readonly RngValidationService _validator;
        private readonly IMinioClient _minio;
        private readonly string _bucketName = "interoperability";
        private readonly string _prefix = "jobpostings-rng/";
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
        public async Task<IActionResult> Index()
        {
            var model = new RngValidationViewModel
            {
                RngData = SampleRng(),
                JobPostings = await ListJobPostingsAsync()
            };
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
                model.JobPostings = await ListJobPostingsAsync();
                return View(model);
            }

            var errors = _validator.ValidateXmlWithRngString(model.XmlData, model.RngData);

            if (errors.Count == 0)
            {
                try
                {
                   
                    bool bucketExists = await _minio.BucketExistsAsync(
                        new BucketExistsArgs().WithBucket(_bucketName));
                    if (!bucketExists)
                    {
                        await _minio.MakeBucketAsync(
                            new MakeBucketArgs().WithBucket(_bucketName));
                    }

                   
                    var objectName = $"{_prefix}{Guid.NewGuid()}.xml";
                    await SaveToMinio(model.XmlData, objectName);

                    model.Result = "<span style='color:green;'>XML is valid and saved to MinIO!</span>";
                }
                catch (Exception ex)
                {
                    model.Result = $"<span style='color:red;'>Error: {ex.Message}</span>";
                }
            }
            else
            {
                model.Result = "<span style='color:red;'>Validation errors:<br/>" +
                              string.Join("<br/>", errors) + "</span>";
            }

            model.JobPostings = await ListJobPostingsAsync();
            return View(model);
        }

        private async Task SaveToMinio(string xmlData, string objectName)
        {
            var xmlBytes = Encoding.UTF8.GetBytes(xmlData);
            using var stream = new MemoryStream(xmlBytes);

            await _minio.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectName)
                    .WithStreamData(stream)
                    .WithObjectSize(stream.Length)
                    .WithContentType("application/xml"));
        }

        private async Task<List<JobPosting>> ListJobPostingsAsync()
        {
            var postings = new List<JobPosting>();
            try
            {
                bool bucketExists = await _minio.BucketExistsAsync(
                    new BucketExistsArgs().WithBucket(_bucketName));
                if (!bucketExists) return postings;

                var listArgs = new ListObjectsArgs()
                    .WithBucket(_bucketName)
                    .WithPrefix(_prefix)
                    .WithRecursive(true);

                await foreach (var item in _minio.ListObjectsEnumAsync(listArgs))
                {
                    if (!item.Key.EndsWith(".xml")) continue;

                    using var ms = new MemoryStream();
                    await _minio.GetObjectAsync(
                        new GetObjectArgs()
                            .WithBucket(_bucketName)
                            .WithObject(item.Key)
                            .WithCallbackStream(async stream => await stream.CopyToAsync(ms)));

                    ms.Seek(0, SeekOrigin.Begin);
                    var xmlContent = Encoding.UTF8.GetString(ms.ToArray());

                    
                    try
                    {
                        var serializer = new XmlSerializer(typeof(JobPostingsWrapper));
                        using var reader = new StringReader(xmlContent);
                        var wrapper = (JobPostingsWrapper)serializer.Deserialize(reader);
                        if (wrapper?.Items != null)
                            postings.AddRange(wrapper.Items);
                    }
                    catch
                    {
                        
                    }
                }
            }
            catch
            {
                
            }
            return postings;
        }

        private string SampleXml()
        {
            var newId = Guid.NewGuid().ToString();
            return $@"<JobPostings>
  <JobPosting>
    <Id>{newId}</Id>
    <RestId>1766819008026931200</RestId>
    <Title>Senior Python Developer</Title>
    <ExternalUrl>https://jobs.smartrecruiters.com/Devoteam/743999972803753-senior-python-developer</ExternalUrl>
    <JobPageUrl>https://x.com/i/jobs/1766819008026931200</JobPageUrl>
    <Location>Machelen, Vlaams Gewest, be</Location>
    <CompanyName>Devoteam</CompanyName>
    <CompanyLogoUrl>https://pbs.twimg.com/profile_images/1082991650794889217/h4Bo8Z5E_normal.jpg</CompanyLogoUrl>
  </JobPosting>
</JobPostings>";
        }

        private string SampleRng() => @"<grammar xmlns='http://relaxng.org/ns/structure/1.0'>
  <start>
    <element name='JobPostings'>
      <oneOrMore>
        <element name='JobPosting'>
          <element name='Id'><text/></element>
          <element name='RestId'><text/></element>
          <element name='Title'><text/></element>
          <element name='ExternalUrl'><text/></element>
          <element name='JobPageUrl'><text/></element>
          <element name='Location'><text/></element>
          <element name='CompanyName'><text/></element>
          <element name='CompanyLogoUrl'><text/></element>
        </element>
      </oneOrMore>
    </element>
  </start>
</grammar>";

     
        [XmlRoot("JobPostings")]
        public class JobPostingsWrapper
        {
            [XmlElement("JobPosting")]
            public List<JobPosting> Items { get; set; } = new List<JobPosting>();
        }

    }
}


