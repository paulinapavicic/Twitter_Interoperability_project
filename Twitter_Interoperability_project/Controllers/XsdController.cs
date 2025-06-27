using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.ApiEndpoints;
using Minio.DataModel.Args;
using System.Text;
using System.Xml.Serialization;
using Twitter_Interoperability_project.Models;
using Twitter_Interoperability_project.Service;

namespace Twitter_Interoperability_project.Controllers
{
    public class XsdController : Controller
    {
        private readonly XmlValidationService _validator;
        private readonly IMinioClient _minio;
        private readonly string _bucketName = "interoperability";
        private readonly string _xsdPath = "wwwroot/schema/jobpostings.xsd"; 

        public XsdController(IConfiguration configuration)
        {
            _validator = new XmlValidationService();

            
            _minio = new MinioClient()
                .WithEndpoint(configuration["MinIO:Endpoint"])
                .WithCredentials(configuration["MinIO:AccessKey"], configuration["MinIO:SecretKey"])
                .Build();
        }

        [XmlRoot("JobPostings")]
        public class JobPostingsWrapper
        {
            [XmlElement("JobPosting")]
            public List<JobPosting> Items { get; set; } = new List<JobPosting>();
        }


        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new XsdValidationViewModel();
            model.JobPostings = await ListJobPostingsAsync();
            model.XsdData = System.IO.File.ReadAllText(_xsdPath);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(XsdValidationViewModel model, string submit)
        {
            model.XsdData = System.IO.File.ReadAllText(_xsdPath);

            if (submit == "Sample")
            {
                
                model.XmlData = SampleXml();
                model.Result = null;
                model.JobPostings = await ListJobPostingsAsync();
                return View(model);
            }

            
            string xsdContent = System.IO.File.ReadAllText(_xsdPath);

            
            var errors = _validator.ValidateXmlWithXsdString(model.XmlData, xsdContent);

            if (errors.Count == 0)
            {
                try
                {
                    
                    var objectName = $"jobpostings/{Guid.NewGuid()}.xml";
                    await SaveToMinio(model.XmlData, objectName);

                    var job = DeserializeJobPostings(model.XmlData);
                    model.JobPostings = await ListJobPostingsAsync();


                    model.Result = "<span style='color:green;'>XML is valid and saved to MinIO!</span>";
                }
                catch (Exception ex)
                {
                    model.Result = $"<span style='color:red;'>Error: {ex.Message}</span>";
                    model.JobPostings = await ListJobPostingsAsync();
                }
            }
            else
            {
                model.Result = "<span style='color:red;'>Validation errors:<br/>" +
                              string.Join("<br/>", errors) + "</span>";
                model.JobPostings = await ListJobPostingsAsync();
            }

            return View(model);
        }

        private async Task SaveToMinio(string xmlData, string objectName)
        {
            var xmlBytes = Encoding.UTF8.GetBytes(xmlData);
            using var stream = new MemoryStream(xmlBytes);

           
            bool bucketExists = await _minio.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_bucketName));

            if (!bucketExists)
            {
                await _minio.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(_bucketName));
            }

            
            await _minio.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectName)
                    .WithStreamData(stream)
                    .WithObjectSize(stream.Length)
                    .WithContentType("application/xml"));
        }

        private JobPostingsWrapper DeserializeJobPostings(string xmlData)
        {
            var serializer = new XmlSerializer(typeof(JobPostingsWrapper));
            using var reader = new StringReader(xmlData);
            return (JobPostingsWrapper)serializer.Deserialize(reader);
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
                    .WithPrefix("jobpostings/")
                    .WithRecursive(true);

                await foreach (var item in _minio.ListObjectsEnumAsync(listArgs))
                {
                    if (!item.Key.EndsWith(".xml")) continue;

                    using var ms = new MemoryStream();
                    await _minio.GetObjectAsync(
                        new GetObjectArgs()
                            .WithBucket(_bucketName)
                            .WithObject(item.Key)
                            .WithCallbackStream(async stream =>
                            {
                                await stream.CopyToAsync(ms); 
                            }));

                    ms.Seek(0, SeekOrigin.Begin);
                    var xmlContent = Encoding.UTF8.GetString(ms.ToArray());
                    var wrapper = DeserializeJobPostings(xmlContent);
                    postings.AddRange(wrapper.Items);
                }
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"Error listing jobs: {ex.Message}");
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


    }
}


