using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
using System.Text;
using System.Xml.Serialization;
using Twitter_Interoperability_project.Models;
using Twitter_Interoperability_project.Service;

namespace Twitter_Interoperability_project.Controllers
{
    [ApiController]
    [Route("api/jobpostings")]
    public class JobPostingsApiController : Controller
    {
        private readonly XmlValidationService _validator;
        private readonly IMinioClient _minio;
        private readonly string _bucketName = "interoperability";
        private readonly string _xsdPath = "wwwroot/schema/jobposting.xsd";

        public JobPostingsApiController(IConfiguration configuration)
        {
            _validator = new XmlValidationService();
            _minio = new MinioClient()
                .WithEndpoint(configuration["MinIO:Endpoint"])
                .WithCredentials(configuration["MinIO:AccessKey"], configuration["MinIO:SecretKey"])
                .Build();
        }

        // GET: api/jobpostings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<JobPosting>>> GetAll()
        {
            var jobs = await ListJobPostingsAsync();
            return Ok(jobs);
        }

        // GET: api/jobpostings/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<JobPosting>> Get(string id)
        {
            var jobs = await ListJobPostingsAsync();
            var job = jobs.FirstOrDefault(j => j.Id == id);
            if (job == null) return NotFound();
            return Ok(job);
        }

        // POST: api/jobpostings
        [HttpPost]
        public async Task<ActionResult<JobPosting>> Create([FromBody] JobPosting job)
        {
            if (job == null)
                return BadRequest();

            var jobs = await ListJobPostingsAsync();
            if (!string.IsNullOrEmpty(job.Id) && jobs.Any(j => j.Id == job.Id))
                return Conflict($"JobPosting with Id '{job.Id}' already exists.");

            job.Id = job.Id ?? Guid.NewGuid().ToString();

            // Serialize and save to MinIO
            var serializer = new XmlSerializer(typeof(JobPosting));
            string xmlContent;
            using (var sw = new StringWriter())
            {
                serializer.Serialize(sw, job);
                xmlContent = sw.ToString();
            }
            var objectName = $"jobpostings/{job.Id}.xml";
            await SaveToMinio(xmlContent, objectName);

            return CreatedAtAction(nameof(Get), new { id = job.Id }, job);
        }

        // POST: api/jobpostings/upload-xml
        
        [HttpPost("upload-xml")]
        public async Task<IActionResult> UploadXml([FromForm] IFormFile xmlFile)
        {
            if (xmlFile == null || xmlFile.Length == 0)
                return BadRequest("No file uploaded.");

            string xmlContent;
            using (var reader = new StreamReader(xmlFile.OpenReadStream()))
            {
                xmlContent = await reader.ReadToEndAsync();
            }

            string xsdContent = System.IO.File.ReadAllText(_xsdPath);
            var errors = _validator.ValidateXmlWithXsdString(xmlContent, xsdContent);

            if (errors.Count > 0)
                return BadRequest(new { Errors = errors });

            // Deserialize to get the Id
            var serializer = new XmlSerializer(typeof(JobPosting));
            JobPosting job;
            using (var sr = new StringReader(xmlContent))
            {
                job = (JobPosting)serializer.Deserialize(sr);
            }

            var objectName = $"jobpostings/{job.Id ?? Guid.NewGuid().ToString()}.xml";
            await SaveToMinio(xmlContent, objectName);

            return Ok(new { Message = "XML file validated and saved.", Job = job });
        }

        // PUT: api/jobpostings/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] JobPosting updated)
        {
            var jobs = await ListJobPostingsAsync();
            var job = jobs.FirstOrDefault(j => j.Id == id);
            if (job == null) return NotFound();

            // Update fields
            updated.Id = id;
            var serializer = new XmlSerializer(typeof(JobPosting));
            string xmlContent;
            using (var sw = new StringWriter())
            {
                serializer.Serialize(sw, updated);
                xmlContent = sw.ToString();
            }

            var objectName = $"jobpostings/{id}.xml";
            await SaveToMinio(xmlContent, objectName);

            return NoContent();
        }

        // DELETE: api/jobpostings/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var objectName = $"jobpostings/{id}.xml";
            try
            {
                await _minio.RemoveObjectAsync(
                    new RemoveObjectArgs()
                        .WithBucket(_bucketName)
                        .WithObject(objectName));
            }
            catch
            {
                return NotFound();
            }
            return NoContent();
        }

        // Helper: Save XML to MinIO
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

        // Helper: List all JobPostings from MinIO
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
                            .WithCallbackStream(async stream => await stream.CopyToAsync(ms)));

                    ms.Seek(0, SeekOrigin.Begin);
                    var serializer = new XmlSerializer(typeof(JobPosting));
                    using var reader = new StreamReader(ms);
                    var job = (JobPosting)serializer.Deserialize(reader);
                    postings.Add(job);
                }
            }
            catch
            {
                // Ignore errors for listing
            }
            return postings;
        }

    }
}
