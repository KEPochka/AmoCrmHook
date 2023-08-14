using Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp;
using WebApp.Controllers.v1;
using WebApp.DataContext;
using WebApp.Services;

namespace Test
{
  public class DataControllerTest
  {
    private readonly Settings _settings = new()
    {
      Namespace = "Data.NewModels",
      ModelsPath = @"d:\Projects\AmoCrmHook\WebApp\bin\Debug\net6.0\Models\",
      AssemblyPath = @"d:\\Projects\\AmoCrmHook\\WebApp\\bin\\Debug\\net6.0\\"
    };

    private const string ConnectionString = "Host=208.0.0.134; Port=5432; Database=AmoCRM; User ID=postgres; Password=123456; Pooling=true; Persist Security Info=false; Include Error Detail=true";

    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;

    [SetUp]
    public void Setup()
    {
      _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql(ConnectionString)
        .Options;
    }

    [Test]
    public void PaymentsTest()
    {
      var controller = new DataController(_dbOptions, new JsonDeserializer<Payment>(), new JsonDeserializer<Rate>(), new MetaDataEditor(), _settings);

      var paymentJson =
@"{
  ""ete_id"": 8595930,
  ""source"": ""amocrm"",
  ""lead_id"": 30496340,
  ""payments"": [
    {
      ""id"": 2882,
      ""price"": 190000,
      ""comment"": """",
      ""rate_id"": 1020,
      ""event_date"": 1685721303,
      ""event_type"": ""changed"",
      ""actual_date"": 1685480400,
      ""invoice_url"": """",
      ""created_date"": 1685721218,
      ""payment_type"": [
        {
          ""id"": 3,
          ""value"": ""оплата по счету""
        }
      ],
      ""invoice_number"": """",
      ""scheduled_date"": 1685480400,
      ""contract_number"": """",
      ""payment_category"": [
        {
          ""id"": 1,
          ""value"": ""регистрационный взнос""
        }
      ],
      ""responsible_user_id"": 7828929
    },
    {
      ""id"": 2883,
      ""price"": 500000,
      ""comment"": """",
      ""rate_id"": 1020,
      ""event_date"": 1685721303,
      ""event_type"": ""changed"",
      ""actual_date"": null,
      ""invoice_url"": """",
      ""created_date"": 1685721218,
      ""payment_type"": [
        {
          ""id"": 3,
          ""value"": ""оплата по счету""
        }
      ],
      ""invoice_number"": """",
      ""scheduled_date"": 1686776400,
      ""contract_number"": """",
      ""payment_category"": [
        {
          ""id"": 2,
          ""value"": ""членство в клубе""
        }
      ],
      ""responsible_user_id"": 7828929
    }
  ],
  ""contact_id"": 49240468,
  ""event_type"": ""payment"",
  ""lead_status_id"": 47610111,
  ""lead_created_at"": 1679950195,
  ""lead_pipeline_id"": 5309073
}";

      var result = (StatusCodeResult)controller.Put(paymentJson).Result;

      if (result.StatusCode == 200)
        Assert.Pass();
      else
        Assert.Fail();
    }

    [Test]
    public void RateTest()
    {
      var controller = new DataController(_dbOptions, new JsonDeserializer<Payment>(), new JsonDeserializer<Rate>(), new MetaDataEditor(), _settings);

      var rateJson =
@"{
  ""rate"": [
    {
      ""id"": 1020,
      ""name"": ""НОВЫЙ РЕЗИДЕНТ (СПЕЦ. ПРЕДЛОЖЕНИЕ НА МАЙ 190 руб. + 500 руб.) "",
      ""status"": ""активен"",
      ""end_date"": 1717880400,
      ""left_days"": 373,
      ""paid_days"": 0,
      ""start_date"": 1686344400,
      ""paid_by_date"": null,
      ""left_paid_days"": 0,
      ""early_termination_date"": null
    }
  ],
  ""ete_id"": 485859,
  ""source"": ""amocrm"",
  ""lead_id"": 30496340,
  ""contact_id"": 49240468,
  ""event_type"": ""lead_rate_changed"",
  ""lead_status_id"": 47610111,
  ""lead_created_at"": 1679950195,
  ""lead_pipeline_id"": 5309073
}";

      var result = (StatusCodeResult)controller.Put(rateJson).Result;

      if (result.StatusCode == 200)
        Assert.Pass();
      else
        Assert.Fail();
    }
  }
}