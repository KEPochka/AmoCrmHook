using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using WebApp.DynamicTypeDescription;
using WebApp.DynamicTypeGeneration;

namespace WebApp.Extentions
{
    public static class JPropertyHelper
    {
        private static readonly CultureInfo cultureInfo = new CultureInfo("en");

        private static ConcurrentDictionary<string, PropertyDescriptor[]> _PropertyDescriptorCache = new ConcurrentDictionary<string, PropertyDescriptor[]>();

        public static string GetPropertyName(this JProperty prop)
        {
            return cultureInfo.TextInfo.ToTitleCase(prop.Name.ToLower().Replace("_", " ")).Replace(" ", "");
        }

        public static PropertyDescriptor[]? GetProperties(string name)
        {
            _PropertyDescriptorCache.TryGetValue(name, out PropertyDescriptor[]? result);

            return result;
        }

        public static string? SetValueOrAddProperty<TTarget>(this JProperty prop, DynamicPropertyManager<TTarget> propertyManager, PropertyDescriptorCollection properties, TTarget dataObject)
        {
            string? newProp = null;

            var property = properties.Find(prop.Name.Replace("_", string.Empty), true);

            var propValue = (JValue)prop.Value;
            switch (propValue.Type)
            {
                case JTokenType.Integer:
                    {
                        var value = propValue.Value<int>();
                        if (property == null)
                        {
                            newProp = prop.GetPropertyName();
                            //TODO: Более осмысленного решения пока не вижу.
                            if (newProp.EndsWith("Date"))
                            {
                                var dt = (new DateTime(1970, 01, 01)).AddSeconds(value);
                                propertyManager.Properties.Add(DynamicPropertyManager<TTarget>.CreateProperty<TTarget, DateTime>(newProp, t => dt, null));
                            }
                            else
                                propertyManager.Properties.Add(DynamicPropertyManager<TTarget>.CreateProperty<TTarget, int>(newProp, t => value, null));
                        }
                        else
                        {
                            var valueType = Nullable.GetUnderlyingType(property.PropertyType);
                            if (valueType == null)
                                valueType = property.PropertyType;

                            if (valueType == typeof(int))
                                property.SetValue(dataObject, value);
                            else if (valueType == typeof(long))
                                property.SetValue(dataObject, Convert.ToInt64(value));
                            else if (valueType == typeof(decimal))
                                property.SetValue(dataObject, Convert.ToDecimal(value));
                            else if (valueType == typeof(double))
                                property.SetValue(dataObject, Convert.ToDouble(value));
                            else if (valueType == typeof(DateTime)) {
                                var dt = (new DateTime(1970, 01, 01)).AddSeconds(value);
                                property.SetValue(dataObject, dt);
                            }
                        }
                    }
                    break;

                case JTokenType.Float:
                    {
                        var value = propValue.Value<decimal>();
                        if (property == null)
                        {
                            newProp = prop.GetPropertyName();
                            propertyManager.Properties.Add(DynamicPropertyManager<TTarget>.CreateProperty<TTarget, decimal>(newProp, t => value, null));
                        }
                        else
                            property.SetValue(dataObject, property.Converter.ConvertFrom(value));
                    }
                    break;

                case JTokenType.Boolean:
                    {
                        var value = propValue.Value<bool>();
                        if (property == null)
                        {
                            newProp = prop.GetPropertyName();
                            propertyManager.Properties.Add(DynamicPropertyManager<TTarget>.CreateProperty<TTarget, bool>(newProp, t => value, null));
                        }
                        else
                            property.SetValue(dataObject, property.Converter.ConvertFrom(value));
                    }
                    break;

                case JTokenType.String:
                    {
                        var value = propValue.Value<string>();
                        if (property == null)
                        {
                            newProp = prop.GetPropertyName();
                            propertyManager.Properties.Add(DynamicPropertyManager<TTarget>.CreateProperty<TTarget, string>(newProp, t => value, null));
                        }
                        else
                            property.SetValue(dataObject, value);
                    }
                    break;

                case JTokenType.Null:
                    {
                        if (property == null)
                        {
                            //TODO: Не ясно - откуда взять тип свойства.
                        }
                        // Если свойство уже существует, то ничего делать не надо.
                    }
                    break;

                default: throw new InvalidDataException("Unsupported token type.");
            }

            return newProp;
        }

        public static void SetValue<TTarget>(this JProperty prop, PropertyDescriptorCollection properties, TTarget dataObject)
        {
            var propValue = (JValue)prop.Value;
            switch (propValue.Type)
            {
                case JTokenType.Integer:
                    {
                        var value = propValue.Value<int>();
                        var property = properties.Find(prop.Name, true);
                        if (property == null)
                        {
                            throw new InvalidDataException($"Property {prop.Name} is absent.");
                        }
                        else
                        {
                            var valueType = Nullable.GetUnderlyingType(property.PropertyType);
                            if (valueType == null)
                                valueType = property.PropertyType;

                            if (valueType == typeof(int))
                                property.SetValue(dataObject, value);
                            else if (valueType == typeof(long))
                                property.SetValue(dataObject, Convert.ToInt64(value));
                            else if (valueType == typeof(decimal))
                                property.SetValue(dataObject, Convert.ToDecimal(value));
                            else if (valueType == typeof(double))
                                property.SetValue(dataObject, Convert.ToDouble(value));
                            else if (valueType == typeof(DateTime)) {
                                var dt = (new DateTime(1970, 01, 01)).AddSeconds(value);
                                property.SetValue(dataObject, dt);
                            }
                        }
                    }
                    break;

                case JTokenType.Float:
                    {
                        var value = propValue.Value<decimal>();
                        var property = properties.Find(prop.Name, true);
                        if (property == null)
                        {
                            throw new InvalidDataException($"Property {prop.Name} is absent.");
                        }
                        else
                            property.SetValue(dataObject, property.Converter.ConvertFrom(value));
                    }
                    break;

                case JTokenType.Boolean:
                    {
                        var value = propValue.Value<bool>();
                        var property = properties.Find(prop.Name, true);
                        if (property == null)
                        {
                            throw new InvalidDataException($"Property {prop.Name} is absent.");
                        }
                        else
                            property.SetValue(dataObject, property.Converter.ConvertFrom(value));
                    }
                    break;

                case JTokenType.String:
                    {
                        var value = propValue.Value<string>();
                        var property = properties.Find(prop.Name, true);
                        if (property == null)
                        {
                            throw new InvalidDataException($"Property {prop.Name} is absent.");
                        }
                        else
                            property.SetValue(dataObject, value);
                    }
                    break;

                case JTokenType.Null:
                    {
                        if (properties.Find(prop.Name, true) == null)
                            throw new InvalidDataException($"Property {prop.Name} is absent.");
                    }
                    break;

                default: throw new InvalidDataException("Unsupported token type.");
            }
        }

        public static PropertyDescriptor[] GenerateTemporaryType(this JProperty prop, out Type returnType)
        {
            if (prop.Value.GetType() != typeof(JArray))
                throw new InvalidCastException("Wrong property type.");

            var name = prop.GetPropertyName();

            AssemblyName asmName = new AssemblyName();
            asmName.Name = name + "Assembly";

            AssemblyBuilder asmBuild = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder modBuild = asmBuild.DefineDynamicModule(name + "Module");
            TypeBuilder tb = modBuild.DefineType(name, TypeAttributes.Public);

            Type? objType = tb.CreateType();
            if (objType == null)
                throw new InvalidProgramException($"Cann't create type '{name}'.");

            var returnObj = Activator.CreateInstance(objType);
            if (returnObj == null)
                throw new InvalidProgramException($"Cann't create object of type '{name}'.");

            returnType = objType;

            var provider = new DynamicTypeDescriptionProvider(returnType);
            TypeDescriptor.AddProvider(provider, returnObj);

            var childObj = ((JArray)prop.Value).ToArray().First();
            foreach (JProperty childProp in ((JObject)childObj).Properties())
            {
                var newProp = childProp.GetPropertyName();
                var propValue = (JValue)childProp.Value;

                switch (propValue.Type)
                {
                    case JTokenType.Integer:
                        {
                            var value = propValue.Value<long>();
                            //TODO: Более осмысленного решения пока не вижу.
                            if (newProp.EndsWith("Date"))
                            {
                                var dt = (new DateTime(1970, 01, 01)).AddSeconds(value);
                                provider.Properties.AddProperty(newProp, dt, returnObj);
                            }
                            else
                            {
                                provider.Properties.AddProperty(newProp, value, returnObj);
                            }
                        }
                        break;

                    case JTokenType.Float:
                        {
                            var value = propValue.Value<decimal>();
                            provider.Properties.AddProperty(newProp, value, returnObj);
                        }
                        break;

                    case JTokenType.Boolean:
                        {
                            var value = propValue.Value<bool>();
                            provider.Properties.AddProperty(newProp, value, returnObj);
                        }
                        break;

                    case JTokenType.String:
                        {
                            var value = propValue.Value<string>();
                            provider.Properties.AddProperty(newProp, value, returnObj);
                        }
                        break;

                    default: throw new InvalidDataException("Unsupported token type.");
                }
            }

            var result = provider.Properties.ToArray();
            TypeDescriptor.RemoveProvider(provider, returnObj);
            return result;
        }

        public static string? AddObjProperty<TTarget, TProperty>(this JProperty prop, DynamicPropertyManager<TTarget> propertyManager, TProperty newObject, long id)
        {
            string? newProp = prop.GetPropertyName();
            propertyManager.Properties.Add(DynamicPropertyManager<TTarget>.CreateProperty<TTarget, long>(newProp + "Id", t => id, null));
            propertyManager.Properties.Add(DynamicPropertyManager<TTarget>.CreateProperty<TTarget, TProperty>(newProp, t => newObject, null));

            return newProp;
        }

        public static void SetPropertyValue<TTarget>(this JProperty prop, PropertyDescriptorCollection properties, DynamicPropertyManager<TTarget> propertyManager, TTarget trgObject, string modelsPath, string namespaceName, List<string> newProps)
        {
            if (prop.Value.GetType() == typeof(JArray))
            {
                var fndProp = properties.Find(prop.Name.Replace("_", string.Empty), true);
                if (fndProp != null)
                {
                    var childObject = Activator.CreateInstance(fndProp.PropertyType) ?? throw new InvalidProgramException($"Cann't create type '{fndProp.PropertyType}'.");
                    fndProp.SetValue(trgObject, childObject);
                    var childProperties = TypeDescriptor.GetProperties(fndProp.PropertyType);
                    var fndPropId = properties.Find(fndProp.Name + "id", true);

                    foreach (var (chld_prop, fndChildProp) in from JProperty obj_prop in prop.Value.SelectMany(obj => obj.ToArray<dynamic>())
                                                              let fndChildProp = childProperties.Find(obj_prop.Name.Replace("_", string.Empty), true)
                                                              select (obj_prop, fndChildProp))
                    {
                        if (chld_prop.Name.ToLower() == "id")
                        {
                            var val = chld_prop.Value.Value<long>();
                            fndPropId?.SetValue(trgObject, val);
                            fndChildProp.SetValue(childObject, val);
                        }
                        else
                        {
                            chld_prop.SetValue(childProperties, childObject);
                        }
                    }
                }
                else
                {
                    var props = prop.GenerateTemporaryType(out Type childObjType);
                    props.GenerateClass(modelsPath, namespaceName, prop.GetPropertyName());

                    var idProp = props.FirstOrDefault(x => x.Name == "Id");
                    if (idProp == null)
                        throw new InvalidDataException("Property 'Id' is absent.");

                    var objVal = idProp.GetValue(childObjType);
                    if (objVal == null)
                        throw new InvalidDataException("Property 'Id' is null.");

                    var newProp = prop.AddObjProperty(propertyManager, childObjType, (long)objVal);
                    if (newProp != null && !newProps.Contains(newProp))
                    {
                        newProps.Add(newProp);
                        _PropertyDescriptorCache.AddOrUpdate($"{newProp}{objVal}", props, (k, v) => { return v; });
                    }
                }
            }
            else if (prop.Value.GetType() == typeof(JValue))
            {
                var newProp = prop.SetValueOrAddProperty(propertyManager, properties, trgObject);
                if (newProp != null && !newProps.Contains(newProp))
                    newProps.Add(newProp);
            }
            else
            {
                throw new InvalidDataException("Unexpected value type.");
            }
        }
    }
}