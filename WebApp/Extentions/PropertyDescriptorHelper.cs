using System.Collections.Concurrent;
using System.ComponentModel;
using WebApp.DynamicTypeDescription;

namespace WebApp.Extentions
{
    public static class PropertyDescriptorHelper
    {
        private class CacheKey
        {
            public string? objName { get; set; }

            public long objId { get; set; }

            public override int GetHashCode()
            {
                var res = 0;
                if (!string.IsNullOrEmpty(this.objName))
                {
                    foreach (char ch in this.objName)
                        res += ch;
                }
                return Convert.ToInt32($"{res}{this.objId}");
            }
        }

        private static ConcurrentDictionary<int, object?> _ObjInstancesCache = new ConcurrentDictionary<int, object?>();

        public static void AddProperty<TTarget, TProperty>(this IList<PropertyDescriptor> Properties, string name, TProperty val, TTarget obj)
        {
            Properties.Add(DynamicPropertyManager<TTarget>.CreateProperty<TTarget, TProperty>(name, t => val, null));
        }

        public static object GetNewObject(this PropertyDescriptor property, object srcObj, PropertyDescriptor[] srcProps, Type newObjType, out long id)
        {
            var idPropName = property.Name + "Id";
            var idProp = srcProps.FirstOrDefault(x => x.Name == idPropName);
            if (idProp == null)
                throw new InvalidDataException($"Property {idProp} not found.");

            var idObj = idProp.GetValue(srcObj);
            if (idObj == null)
                throw new InvalidDataException($"Property {idProp} is null.");

            id = (long)idObj;

            var key = new CacheKey
            {
                objName = property.Name,
                objId = id
            };

            var obj = _ObjInstancesCache.GetOrAdd(key.GetHashCode(), Activator.CreateInstance(newObjType));
            if (obj == null)
                throw new InvalidOperationException($"Cann't create type '{0}'.");

            return obj;
        }
    }
}
