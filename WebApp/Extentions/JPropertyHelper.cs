using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using WebApp.DynamicTypeDescription;
using WebApp.DynamicTypeGeneration;
using WebApp.Services;

namespace WebApp.Extentions
{
    public static class JPropertyHelper
    {
        private static readonly CultureInfo CultureInfo = new("en");

        public static void SetPropertyValue<TTarget>(this JProperty prop, PropertyDescriptorCollection properties, DynamicPropertyManager<TTarget> propertyManager, TTarget trgObject, string modelsPath, string namespaceName, List<string> newProps, IPropertiesCache cache)
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

                    foreach (var (chldProp, fndChildProp) in from JProperty objProp in prop.Value.SelectMany(obj => obj.ToArray<dynamic>())
                                                             let fndChildProp = childProperties.Find(objProp.Name.Replace("_", string.Empty), true)
                                                             select (objProp, fndChildProp))
                    {
                        if (chldProp.Name.ToLower() == "id")
                        {
                            var val = chldProp.Value.Value<long>();
                            fndPropId?.SetValue(trgObject, val);
                            fndChildProp.SetValue(childObject, val);
                        }
                        else
                        {
                            chldProp.SetValue(childProperties, childObject);
                        }
                    }
                }
                else
                {
                    var props = prop.GenerateTemporaryType(out var childObjType);
                    props.GenerateClass(modelsPath, namespaceName, prop.GetPropertyName());

                    var idProp = props.FirstOrDefault(x => x.Name == "Id") ?? throw new InvalidDataException("Property 'Id' is absent.");
                    var objVal = idProp.GetValue(childObjType) ?? throw new InvalidDataException("Property 'Id' is null.");
                    var newProp = prop.AddObjProperty(propertyManager, childObjType, (long)objVal);
                    if (!newProps.Contains(newProp))
                    {
                        newProps.Add(newProp);
                        cache.AddOrUpdate($"{newProp}{objVal}", props);
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

        private static string GetPropertyName(this JProperty prop)
        {
            return CultureInfo.TextInfo.ToTitleCase(prop.Name.ToLower().Replace("_", " ")).Replace(" ", "");
        }

        private static string? SetValueOrAddProperty<TTarget>(this JProperty prop, DynamicPropertyManager<TTarget> propertyManager, PropertyDescriptorCollection properties, TTarget dataObject)
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
                            if (newProp.EndsWith("Date"))
                            {
                                var dt = (new DateTime(1970, 01, 01, 0, 0, 0, DateTimeKind.Utc)).AddSeconds(value);
                                propertyManager.Properties.Add(DynamicPropertyManager<TTarget>.CreateProperty<TTarget, DateTime>(newProp, _ => dt, null));
                            }
                            else if (newProp.EndsWith("Id"))
                                propertyManager.Properties.Add(DynamicPropertyManager<TTarget>.CreateProperty<TTarget, long>(newProp, _ => value, null));
                            else
                                propertyManager.Properties.Add(DynamicPropertyManager<TTarget>.CreateProperty<TTarget, int>(newProp, _ => value, null));
                        }
                        else
                        {
                            var valueType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                            if (valueType == typeof(int))
                                property.SetValue(dataObject, value);
                            else if (valueType == typeof(long))
                                property.SetValue(dataObject, Convert.ToInt64(value));
                            else if (valueType == typeof(decimal))
                                property.SetValue(dataObject, Convert.ToDecimal(value));
                            else if (valueType == typeof(double))
                                property.SetValue(dataObject, Convert.ToDouble(value));
                            else if (valueType == typeof(DateTime)) {
                                var dt = (new DateTime(1970, 01, 01, 0, 0, 0, DateTimeKind.Utc)).AddSeconds(value);
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
                            propertyManager.Properties.Add(DynamicPropertyManager<TTarget>.CreateProperty<TTarget, decimal>(newProp, _ => value, null));
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
                            propertyManager.Properties.Add(DynamicPropertyManager<TTarget>.CreateProperty<TTarget, bool>(newProp, _ => value, null));
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
                            propertyManager.Properties.Add(DynamicPropertyManager<TTarget>.CreateProperty<TTarget, string>(newProp, _ => value, null));
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

        private static void SetValue<TTarget>(this JProperty prop, PropertyDescriptorCollection properties, TTarget dataObject)
        {
            var propValue = (JValue)prop.Value;
            switch (propValue.Type)
            {
                case JTokenType.Integer:
                    {
                        var value = propValue.Value<int>();
                        var property = properties.Find(prop.Name, true) ?? throw new InvalidDataException($"Property {prop.Name} is absent.");
                        var valueType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                        if (valueType == typeof(int))
                            property.SetValue(dataObject, value);
                        else if (valueType == typeof(long))
                            property.SetValue(dataObject, Convert.ToInt64(value));
                        else if (valueType == typeof(decimal))
                            property.SetValue(dataObject, Convert.ToDecimal(value));
                        else if (valueType == typeof(double))
                            property.SetValue(dataObject, Convert.ToDouble(value));
                        else if (valueType == typeof(DateTime)) {
                            var dt = (new DateTime(1970, 01, 01, 0, 0, 0, DateTimeKind.Utc)).AddSeconds(value);
                            property.SetValue(dataObject, dt);
                        }
                    }
                    break;

                case JTokenType.Float:
                    {
                        var value = propValue.Value<decimal>();
                        var property = properties.Find(prop.Name, true) ?? throw new InvalidDataException($"Property {prop.Name} is absent.");
                        property.SetValue(dataObject, property.Converter.ConvertFrom(value));
                    }
                    break;

                case JTokenType.Boolean:
                    {
                        var value = propValue.Value<bool>();
                        var property = properties.Find(prop.Name, true) ?? throw new InvalidDataException($"Property {prop.Name} is absent.");
                        property.SetValue(dataObject, property.Converter.ConvertFrom(value));
                    }
                    break;

                case JTokenType.String:
                    {
                        var value = propValue.Value<string>();
                        var property = properties.Find(prop.Name, true) ?? throw new InvalidDataException($"Property {prop.Name} is absent.");
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

        private static PropertyDescriptor[] GenerateTemporaryType(this JProperty prop, out Type returnType)
        {
            if (prop.Value.GetType() != typeof(JArray))
                throw new InvalidCastException("Wrong property type.");

            var name = prop.GetPropertyName();

            var asmName = new AssemblyName { Name = name + "Assembly" };

            var asmBuild = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndCollect);
            var modBuild = asmBuild.DefineDynamicModule(name + "Module");
            var tb = modBuild.DefineType(name, TypeAttributes.Public);

            var objType = tb.CreateType() ?? throw new InvalidProgramException($"Cann't create type '{name}'.");
            var returnObj = Activator.CreateInstance(objType) ?? throw new InvalidProgramException($"Cann't create object of type '{name}'.");
            returnType = objType;

            var provider = new DynamicTypeDescriptionProvider(returnType);
            TypeDescriptor.AddProvider(provider, returnObj);

            var childObj = ((JArray)prop.Value).ToArray().First();
            foreach (var childProp in ((JObject)childObj).Properties())
            {
                var newProp = childProp.GetPropertyName();
                var propValue = (JValue)childProp.Value;

                switch (propValue.Type)
                {
                    case JTokenType.Integer:
                        {
                            if (newProp.EndsWith("Date"))
                            {
                                var value = propValue.Value<int>();
                                var dt = (new DateTime(1970, 01, 01, 0, 0, 0, DateTimeKind.Utc)).AddSeconds(value);
                                provider.Properties.AddProperty(newProp, dt, returnObj);
                            }
                            else if (newProp.EndsWith("Id"))
                            {
                                var value = propValue.Value<long>();
                                provider.Properties.AddProperty(newProp, value, returnObj);
                            }
                            else
                            {
                                var value = propValue.Value<int>();
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

        private static string AddObjProperty<TTarget, TProperty>(this JProperty prop, DynamicPropertyManager<TTarget> propertyManager, TProperty newObject, long id)
        {
            var newProp = prop.GetPropertyName();
            propertyManager.Properties.Add(DynamicPropertyManager<TTarget>.CreateProperty<TTarget, long>(newProp + "Id", _ => id, null));
            propertyManager.Properties.Add(DynamicPropertyManager<TTarget>.CreateProperty<TTarget, TProperty>(newProp, _ => newObject, null));

            return newProp;
        }
    }
}