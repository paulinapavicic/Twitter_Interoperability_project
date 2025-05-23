using System.ServiceModel;

namespace Twitter_Interoperability_project.Interfaces
{

    [ServiceContract]
    public interface IJobPostingSoapService
    {
        [OperationContract]
        string SearchJobPostings(string term);
    }
}
