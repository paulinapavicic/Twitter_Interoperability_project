using CookComputing.XmlRpc;
using Twitter_Interoperability_project.Models;

namespace Twitter_Interoperability_project.Interfaces
{
    public interface IWeatherService : IXmlRpcProxy
    {
        [XmlRpcMethod("Weather.getTemperature")]
        CityTemp[] GetTemperature(string cityPart);
    }
}
