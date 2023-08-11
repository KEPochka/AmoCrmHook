using Newtonsoft.Json.Linq;
using System.ComponentModel;
using WebApp.DynamicTypeDescription;
using WebApp.Extentions;

namespace WebApp.Services
{
    public static class JsonDeserializer<TModel> where TModel : class, new()
    {
        public static Dictionary<TModel, PropertyDescriptorCollection> ParseJson(JArray parsingObject, string modelsPath, string namespaceName, out List<string> newProps)
        {
            var objects = new Dictionary<TModel, PropertyDescriptorCollection>();

            newProps = new List<string>();

            foreach (var jTokens in parsingObject)
            {
                var objModel = new TModel();

                using var propertyManager = new DynamicPropertyManager<TModel>();
                var properties = TypeDescriptor.GetProperties(typeof(TModel));

                foreach (var jToken in jTokens)
                {
                    var prop = (JProperty)jToken;
                    prop.SetPropertyValue(properties, propertyManager, objModel, modelsPath, namespaceName, newProps);
                }

                objects.Add(objModel, TypeDescriptor.GetProperties(typeof(TModel)));
            }

            return objects;
        }
    }
}
