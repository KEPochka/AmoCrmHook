using Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using WebApp.DataContext;
using WebApp.DynamicTypeGeneration;
using WebApp.Extentions;
using WebApp.Services;

namespace WebApp.Controllers.v1;

/// <summary>
/// Controller for adding data received from amoCRM.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class DataController : ControllerBase
{
    /// <summary>
    /// Controller constructor.
    /// </summary>
    /// <param name="dbOptions">Database context options.</param>
    /// <param name="paymentDeserializer">Gets all the information from the incoming JSON payment structure.</param>
    /// <param name="rateDeserializer">Gets all the information from the incoming JSON rate structure.</param>
    /// <param name="metaDataEditor">Controls structure in the database.</param>
    /// <param name="settings">Application configuration settings.</param>
    
    public DataController(IDbContextOptions dbOptions, IJsonDeserializer<Payment> paymentDeserializer, IJsonDeserializer<Rate> rateDeserializer, IMetaDataEditor metaDataEditor, Settings settings)
    {
        Settings = settings;
        MetaDataEditor = metaDataEditor;
        RateDeserializer = rateDeserializer;
        PaymentDeserializer = paymentDeserializer;
        DbOptions = (DbContextOptions<ApplicationDbContext>)dbOptions;
    }

    private Settings Settings { get; }
    private IMetaDataEditor MetaDataEditor { get; }
    private IJsonDeserializer<Rate> RateDeserializer { get; }
    private IJsonDeserializer<Payment> PaymentDeserializer { get; }
    private DbContextOptions<ApplicationDbContext> DbOptions { get; }

    /// <summary>
    /// Creates the database and all tables corresponding to the current state of the models.
    /// </summary>
    /// <remarks>The database must be completely missing any tables or the database must be completely absent..</remarks>
    /// <response code="200">The database has been successfully created.</response>
    /// <response code="400">Failed to create database.</response>
    [HttpPost(Name = "CreateDatabase")]
    public async Task<IActionResult> Post()
    {
        try
        {
            await using var dbContext = new ApplicationDbContext(DbOptions);

            await dbContext.Database.EnsureCreatedAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.Message);

            return BadRequest();
        }
    }

    /// <summary>
    /// Parses JSON and inserts data into the database.
    /// </summary>
    /// <remarks>In the absence of the necessary structures in the database, the service will perform the appropriate changes.</remarks>
    /// <response code="200">Data successfully added to the database.</response>
    /// <response code="400">Failed to add data to the database. Possibly invalid incoming JSON.</response>
    /// <param name="json">Incoming JSON according to the amoCRM Send Data Documentation.</param>
    [HttpPut(Name = "PutData")]
    public async Task<IActionResult> Put(string json)
    {
        try
        {
            if (string.IsNullOrEmpty(json))
                throw new InvalidDataException("Empty incoming JSON.");

            var dynamicObject = JsonConvert.DeserializeObject<dynamic>(json) ?? throw new InvalidDataException("The incoming data is not JSON.");

            Dictionary<Payment, PropertyDescriptorCollection>? payments = null;
            Dictionary<Rate, PropertyDescriptorCollection>? rates = null;
            List<string>? newPaymentProperties = null;
            List<string>? newRateProperties = null;

            if (dynamicObject.payments != null)
            {
                payments = await Task.Run(() =>
                {
                    Dictionary<Payment, PropertyDescriptorCollection> result =
                        PaymentDeserializer.ParseJson(dynamicObject.payments, Settings.ModelsPath, Settings.Namespace, out newPaymentProperties);

                    if (newPaymentProperties is { Count: > 0 })
                    {
                        var properties = result.MaxBy(x => x.Value.Count).Value.Cast<PropertyDescriptor>().ToArray();
                        properties.GenerateClass(Settings.ModelsPath, Settings.Namespace, "Payment");
                    }

                    return result;
                });
            }

            if (dynamicObject.rate != null)
            {
                rates = await Task.Run(() =>
                {
                    Dictionary<Rate, PropertyDescriptorCollection> result =
                        RateDeserializer.ParseJson(dynamicObject.rate, Settings.ModelsPath, Settings.Namespace, out newRateProperties);

                    if (newRateProperties is { Count: > 0 })
                    {
                        var properties = result.MaxBy(x => x.Value.Count).Value.Cast<PropertyDescriptor>().ToArray();
                        properties.GenerateClass(Settings.ModelsPath, Settings.Namespace, "Rate");
                    }

                    return result;
                });
            }

            var assemblyPath = Settings.AssemblyPath + "NewData.dll";

            if (newPaymentProperties is { Count: > 0 } || newRateProperties is { Count: > 0 })
            {
                var dir = new DirectoryInfo(Settings.ModelsPath);
                var files = dir.GetFiles("*.cs").Select(x => x.FullName).ToArray();

                if (GenerateSource.CompileCSharpCode(files, assemblyPath))
                    await WriteDataToDb(MetaDataEditor, DbOptions, assemblyPath, payments, (IPropertiesCache)PaymentDeserializer, rates, (IPropertiesCache)RateDeserializer);
                else
                    throw new InvalidOperationException("Cann't compile \"NewData.dll\".");
            }
            else if (System.IO.File.Exists(assemblyPath))
            {
                await WriteDataToDb(MetaDataEditor, DbOptions, assemblyPath, payments, (IPropertiesCache)PaymentDeserializer, rates, (IPropertiesCache)RateDeserializer);
            }
            else
            {
                await using var dbContext = new ApplicationDbContext(DbOptions);

                MetaDataEditor.CodeFirst(dbContext);

                if (payments != null)
                {
                    foreach (var payment in payments)
                        dbContext.AddOrUpdate(payment.Key);
                }

                if (rates != null)
                {
                    foreach (var rate in rates)
                        dbContext.AddOrUpdate(rate.Key);
                }

                await dbContext.SaveChangesAsync();
            }

            return Ok();
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.Message);

            return BadRequest();
        }
    }

    private static void CopyPropertyValues(object srcObj, PropertyDescriptor[] srcProps, object trgObj, PropertyInfo[] trgProps, IPropertiesCache cache)
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
                        var obj = property.GetNewObject(srcObj, srcProps, trgProp.PropertyType, out var id);
                        trgProp.SetValue(trgObj, obj);

                        if (property.PropertyType.Name == "Type")
                        {
                            var props = cache.GetProperties($"{property.Name}{id}");
                            if (props != null)
                                CopyPropertyValues(val, props, obj, obj.GetType().GetProperties(), cache);
                        }
                        else
                        {
                            var props = TypeDescriptor.GetProperties(val.GetType());
                            CopyPropertyValues(val, props.Cast<PropertyDescriptor>().ToArray(), obj, obj.GetType().GetProperties(), cache);
                        }
                    }
                    else
                        trgProp.SetValue(trgObj, val);
                }
            }
        }
    }

    private static Task UpsertDataInTheDatabase<TModel>(ApplicationDbContext dbContext, Type[] types, Dictionary<TModel, PropertyDescriptorCollection> models, IPropertiesCache cache) where TModel : notnull, new()
    {
        return Task.Run(() =>
        {
            var t = typeof(TModel);

            var type = types.FirstOrDefault(x => x.Name == t.Name) ?? throw new InvalidProgramException($"Type '{t.Name}' not found in the new data assembly.");
            foreach (var model in models)
            {
                var obj = Activator.CreateInstance(type) ?? throw new InvalidOperationException($"Cann't create type '{type.FullName}'.");
                var objProperties = obj.GetType().GetProperties();
                var properties = model.Value.Cast<PropertyDescriptor>().ToArray();

                if (model.Key == null)
                    throw new InvalidProgramException($"Main model '{t.FullName}' is null");
                
                CopyPropertyValues(model.Key, properties, obj, objProperties, cache);

                dbContext.AddOrUpdate(obj);
            }
        });
    }

    private static async Task WriteDataToDb(IMetaDataEditor metaDataEditor, DbContextOptions<ApplicationDbContext> dbOptions, string assemblyPath,
        Dictionary<Payment, PropertyDescriptorCollection>? payments, IPropertiesCache paymentCache,
        Dictionary<Rate, PropertyDescriptorCollection>? rates, IPropertiesCache rateCache)
    {
        var assembly = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
        var types = assembly.GetExportedTypes()
            .Where(x => x is { IsClass: true, IsPublic: true, IsAbstract: false } &&
                        x.CustomAttributes.Any(attr => attr.AttributeType == typeof(TableAttribute)))
            .ToArray();

        await using var dbContext = new ApplicationDbContext(dbOptions, new DbContextConfigurator(types));

        metaDataEditor.CodeFirst(dbContext);

        if (payments != null)
            await UpsertDataInTheDatabase(dbContext, types, payments, paymentCache);

        if (rates != null)
            await UpsertDataInTheDatabase(dbContext, types, rates, rateCache);

        await dbContext.SaveChangesAsync();
    }
}