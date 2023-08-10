using Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Reflection;
using WebApp.DataContext;
using WebApp.DataLoader;
using WebApp.Deserializers;
using WebApp.DynamicTypeGeneration;
using WebApp.Extentions;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataController : ControllerBase
    {
        public DataController(IMetaDataEditor metaDataLoader, IDbContextOptions dbOptions, Settings settings)
        {
            Settings = settings;
            MetaDataEditor = metaDataLoader;
            DbOptions = (DbContextOptions<ApplicationDbContext>)dbOptions;
        }

        public IMetaDataEditor MetaDataEditor { get; }
        public Settings Settings { get; }
        public DbContextOptions<ApplicationDbContext> DbOptions { get; }

        [HttpPut(Name = "PutData")]
        public async Task<IActionResult> Put(string jsonString)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonString))
                    throw new InvalidDataException("Empty incoming JSON.");

                var namespaceName = "Data.NewModels";

                var dynamicObject = JsonConvert.DeserializeObject<dynamic>(jsonString) ?? throw new InvalidDataException("The incoming data is not JSON.");

                Dictionary<Payment, PropertyDescriptorCollection>? payments = null;
                Dictionary<Rate, PropertyDescriptorCollection>? rates = null;
                List<string>? newPaymentProperties = null;
                List<string>? newRateProperties = null;

                if (dynamicObject.payments != null)
                {
                    payments = await Task.Run(() =>
                    {
                        Dictionary<Payment, PropertyDescriptorCollection> result = PaymentDeserializer.ParsePayments(dynamicObject, Settings.ModelsPath, namespaceName, out newPaymentProperties);

                        if (newPaymentProperties != null && newPaymentProperties.Count > 0)
                        {
                            var properties = result.MaxBy(x => x.Value.Count).Value.Cast<PropertyDescriptor>().ToArray();
                            properties.GenerateClass(Settings.ModelsPath, namespaceName, "Payment");
                        }

                        return result;
                    });
                }

                if (dynamicObject.rate != null)
                {
                    rates = await Task.Run(() =>
                    {
                        Dictionary<Rate, PropertyDescriptorCollection> result = RateDeserializer.ParseRates(dynamicObject, Settings.ModelsPath, namespaceName, out newRateProperties);

                        if (newRateProperties != null && newRateProperties.Count > 0)
                        {
                            var properties = result.MaxBy(x => x.Value.Count).Value.Cast<PropertyDescriptor>().ToArray();
                            properties.GenerateClass(Settings.ModelsPath, namespaceName, "Rate");
                        }

                        return result;
                    });
                }

                if ((newPaymentProperties != null && newPaymentProperties.Count > 0) || (newRateProperties != null && newRateProperties.Count > 0))
                {
                    var dir = new DirectoryInfo(Settings.ModelsPath);
                    var files = dir.GetFiles("*.cs").Select(x => x.FullName).ToArray();
                    var assemblyPath = Settings.AssemblyPath + "NewData.dll";

                    if (GenerateSource.CompileCSharpCode(files, assemblyPath))
                    {
                        var assembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
                        var types = assembly.GetExportedTypes()
                            .Where(x => !x.IsAbstract)
                            .ToArray();

                        using var dbContext = new ApplicationDbContext(DbOptions, new DbContextConfigurator(types));

                        MetaDataEditor.CodeFirst(dbContext);

                        if (payments != null)
                        {
                            var newPymentType = types.FirstOrDefault(x => x.Name == "Payment");
                            if (newPymentType == null)
                                throw new InvalidProgramException("Type 'Payment' not found in the new data assembly.");

                            foreach (var payment in payments)
                            {
                                var obj = Activator.CreateInstance(newPymentType);
                                if (obj == null)
                                    throw new InvalidOperationException($"Cann't create type '{newPymentType.FullName}'.");

                                var objProperties = obj.GetType().GetProperties();
                                var properties = payment.Value.Cast<PropertyDescriptor>().ToArray();

                                CopyPropertyValues(payment.Key, properties, obj, objProperties);

                                //TODO: Upsert
                                dbContext.Add(obj);
                            }
                        }

                        if (rates != null)
                        {
                            var newRateType = types.FirstOrDefault(x => x.Name == "Rate");
                            if (newRateType == null)
                                throw new InvalidProgramException("Type 'Rate' not found in the new data assembly.");

                            foreach (var rate in rates)
                            {
                                var obj = Activator.CreateInstance(newRateType);
                                if (obj == null)
                                    throw new InvalidOperationException($"Cann't create type '{newRateType.FullName}'.");

                                var objProperties = obj.GetType().GetProperties();
                                var properties = rate.Value.Cast<PropertyDescriptor>().ToArray();

                                CopyPropertyValues(rate.Key, properties, obj, objProperties);

                                //TODO: Upsert
                                dbContext.Add(obj);
                            }
                        }

                        dbContext.SaveChanges();
                    }
                }
                else
                {
                    using var dbContext = new ApplicationDbContext(DbOptions);

                    MetaDataEditor.CodeFirst(dbContext);

                    if (payments != null)
                    {
                        foreach (var payment in payments)
                        {
                            dbContext.Add(payment.Key);
                        }
                    }

                    if (rates != null)
                    {
                        foreach (var rate in rates)
                        {
                            dbContext.Add(rate.Key);
                        }
                    }

                    dbContext.SaveChanges();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);

                return BadRequest();
            }
        }

        private void CopyPropertyValues(object srcObj, PropertyDescriptor[] srcProps, object trgObj, PropertyInfo[] trgProps)
        {
            foreach (var property in srcProps)
            {
                var val = property.GetValue(srcObj);
                if (val != null)
                {
                    var trgProp = trgProps.FirstOrDefault(x => x.Name == property.Name);
                    if (trgProp != null)
                    {
                        if (property.Name == property.PropertyType.Name || property.PropertyType.Name == "Type")
                        {
                            var obj = property.GetNewObject(srcObj, srcProps, trgProp.PropertyType, out long id);
                            trgProp.SetValue(trgObj, obj);

                            if (property.PropertyType.Name == "Type")
                            {
                                var props = JPropertyHelper.GetProperties($"{property.Name}{id}");
                                if (props != null)
                                    CopyPropertyValues(val, props, obj, obj.GetType().GetProperties());
                            }
                            else
                            {
                                var props = TypeDescriptor.GetProperties(val.GetType());
                                CopyPropertyValues(val, props.Cast<PropertyDescriptor>().ToArray(), obj, obj.GetType().GetProperties());
                            }
                        }
                        else
                            trgProp.SetValue(trgObj, val);
                    }
                }
            }
        }
    }
}
