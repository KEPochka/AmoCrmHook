using System.Collections.Concurrent;
using System.ComponentModel;
using WebApp.DynamicTypeDescription;

namespace WebApp.Extentions
{
    public static class PropertyDescriptorHelper
    {
        private class CacheKey
        {
            public string? ObjName { get; set; }

            public long ObjId { get; set; }

            public override int GetHashCode()
            {
                var res = 0;
                if (!string.IsNullOrEmpty(this.ObjName))
                    res = this.ObjName.Aggregate(res, (current, ch) => current + ch);

                return Convert.ToInt32($"{res}{this.ObjId}");
            }
        }

        private static readonly ConcurrentDictionary<int, object?> ObjInstancesCache = new();

        public static void AddProperty<TTarget, TProperty>(this IList<PropertyDescriptor> properties, string name, TProperty val, TTarget obj)
        {
            properties.Add(DynamicPropertyManager<TTarget>.CreateProperty<TTarget, TProperty>(name, _ => val, null));
        }

        public static object GetNewObject(this PropertyDescriptor property, object srcObj, PropertyDescriptor[] srcProps, Type newObjType, out long id)
        {
            var idPropName = property.Name + "Id";
            var idProp = srcProps.FirstOrDefault(x => x.Name == idPropName);
            if (idProp == null)
                throw new InvalidDataException($"Property {idProp} not found.");

            var idObj = idProp.GetValue(srcObj) ?? throw new InvalidDataException($"Property {idProp} is null.");
            id = (long)idObj;

            var key = new CacheKey
            {
                ObjName = property.Name,
                ObjId = id
            };

            var obj = ObjInstancesCache.GetOrAdd(key.GetHashCode(), Activator.CreateInstance(newObjType)) ?? throw new InvalidOperationException($"Cann't create type '{newObjType}'.");
            return obj;
        }
    }
}
