using Microsoft.AspNetCore.Mvc;
using Twitter_Interoperability_project.Models;

namespace Twitter_Interoperability_project.Controllers
{
    [ApiController]
    [Route("api/jobpostings")]
    public class JobPostingsApiController : Controller
    {
        
        private static List<JobPosting> JobPostings = new List<JobPosting>();

        
        [HttpGet]
        public ActionResult<IEnumerable<JobPosting>> GetAll()
        {
            return Ok(JobPostings);
        }


       
        [HttpGet("{id}")]
        public ActionResult<JobPosting> Get(string id)
        {
            var job = JobPostings.FirstOrDefault(j => j.Id == id);
            if (job == null) return NotFound();
            return Ok(job);
        }

       
        [HttpPost]
        public ActionResult<JobPosting> Create([FromBody] JobPosting job)
        {
            if (job == null)
                return BadRequest();

            if (!string.IsNullOrEmpty(job.Id) && JobPostings.Any(j => j.Id == job.Id))
                return Conflict($"JobPosting with Id '{job.Id}' already exists.");

            job.Id = job.Id ?? Guid.NewGuid().ToString();
            JobPostings.Add(job);
            return CreatedAtAction(nameof(Get), new { id = job.Id }, job);
        }

        
        [HttpPut("{id}")]
        public IActionResult Update(string id, [FromBody] JobPosting updated)
        {
            var job = JobPostings.FirstOrDefault(j => j.Id == id);
            if (job == null) return NotFound();

            job.RestId = updated.RestId;
            job.Title = updated.Title;
            job.ExternalUrl = updated.ExternalUrl;
            job.JobDescription = updated.JobDescription;
            job.JobPageUrl = updated.JobPageUrl;
            job.Location = updated.Location;
            job.LocationType = updated.LocationType;
            job.SeniorityLevel = updated.SeniorityLevel;
            job.Team = updated.Team;
            job.CompanyName = updated.CompanyName;
            job.CompanyLogoUrl = updated.CompanyLogoUrl;

            return NoContent();
        }

        
        [HttpDelete("{id}")]
        public IActionResult Delete(string id)
        {
            var job = JobPostings.FirstOrDefault(j => j.Id == id);
            if (job == null) return NotFound();

            JobPostings.Remove(job);
            return NoContent();
        }
    }
}
