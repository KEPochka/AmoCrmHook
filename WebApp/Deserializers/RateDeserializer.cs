using Data.Models;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using WebApp.DynamicTypeDescription;
using WebApp.Extentions;

namespace WebApp.Deserializers
{
    public static class RateDeserializer
    {
        public static Dictionary<Rate, PropertyDescriptorCollection> ParseRates(dynamic dynamicObject, string modelsPath, string namespaceName, out List<string> newProps)
        {
            var objRates = new Dictionary<Rate, PropertyDescriptorCollection>();

            newProps = new List<string>();

            foreach (var Rate in ((JArray)dynamicObject.Rates).ToArray<dynamic>())
            {
                var objRate = new Rate();

                using var propertyManager = new DynamicPropertyManager<Rate>();
                var properties = TypeDescriptor.GetProperties(typeof(Rate));

                foreach (JProperty prop in Rate)
                {
                    prop.SetPropertyValue(properties, propertyManager, objRate, modelsPath, namespaceName, newProps);
                }

                objRates.Add(objRate, TypeDescriptor.GetProperties(typeof(Rate)));
            }

            return objRates;
        }
    }
}
