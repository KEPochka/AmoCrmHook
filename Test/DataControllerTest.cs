using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp;
using WebApp.Controllers;
using WebApp.DataContext;
using WebApp.DataLoader;

namespace Test
{
    public class DataControllerTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void PaymentsTest()
        {
            var settings = new Settings
            {
                ModelsPath = "d:\\Projects\\WebApp\\WebApp\\bin\\Debug\\net6.0\\Models\\",
                AssemblyPath = "d:\\Projects\\WebApp\\WebApp\\bin\\Debug\\net6.0\\"
            };
            var connectionString = "Host=208.0.0.134; Port=5432; Database=AmoCRM; User ID=postgres; Password=123456; Pooling=true; Persist Security Info=false;";
            var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            var loader = new MetaDataEditor();

            var controller = new DataController(loader, dbOptions, settings);

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

            if(result.StatusCode == 200)
                Assert.Pass();
            else
                Assert.Fail();
        }
    }
}