using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using WebApp.DynamicTypeDescription;
using WebApp.Extentions;

namespace WebApp.Services;

public interface IJsonDeserializer<TModel>
{
    public Dictionary<TModel, PropertyDescriptorCollection> ParseJson(JArray parsingObject, string modelsPath, string namespaceName, out List<string> newProps);
}

public interface IPropertiesCache
{
    public void AddOrUpdate(string name, PropertyDescriptor[] properties);

    public PropertyDescriptor[]? GetProperties(string name);
}

public class JsonDeserializer<TModel> : IPropertiesCache, IJsonDeserializer<TModel> where TModel : class, new()
{
    private readonly ConcurrentDictionary<string, PropertyDescriptor[]> _propertyDescriptorCache = new();

    public void AddOrUpdate(string name, PropertyDescriptor[] properties)
    {
        _propertyDescriptorCache.AddOrUpdate(name, properties, (_, v) => v);
    }

    public PropertyDescriptor[]? GetProperties(string name)
    {
        _propertyDescriptorCache.TryGetValue(name, out var result);
        return result;
    }

    public Dictionary<TModel, PropertyDescriptorCollection> ParseJson(JArray parsingObject, string modelsPath, string namespaceName, out List<string> newProps)
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
                prop.SetPropertyValue(properties, propertyManager, objModel, modelsPath, namespaceName, newProps, this);
            }

            objects.Add(objModel, TypeDescriptor.GetProperties(typeof(TModel)));
        }

        return objects;
    }
}