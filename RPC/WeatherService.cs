using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CookComputing.XmlRpc;

namespace RPC
{
   public class WeatherService :XmlRpcListenerService
    {
        [XmlRpcMethod("getTemperature")]
        public CityTemp[] GetTemperature(string cityPart)
        {
            var xmlString = new WebClient().DownloadString("https://vrijeme.hr/hrvatska_n.xml");
            var doc = XDocument.Parse(xmlString);

            var cityPartLower = cityPart.ToLower();
            var matchedCities = doc.Descendants("Grad")
                .Where(g =>
                    g.Element("GradIme") != null &&
                    g.Element("GradIme").Value.ToLower().Contains(cityPartLower))
                .Select(g => new CityTemp
                {
                    Name = g.Element("GradIme").Value,
                    Temp = g.Element("Podatci")?.Element("Temp")?.Value.Trim() ?? "N/A"
                })
                .ToArray();

            return matchedCities;
        }

    }
}
