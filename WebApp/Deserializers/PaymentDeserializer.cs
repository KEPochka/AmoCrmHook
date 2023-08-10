using Data.Models;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using WebApp.DynamicTypeDescription;
using WebApp.Extentions;

namespace WebApp.Deserializers
{
    public static class PaymentDeserializer
    {
        public static Dictionary<Payment, PropertyDescriptorCollection> ParsePayments(dynamic dynamicObject, string modelsPath, string namespaceName, out List<string> newProps)
        {
            var objPayments = new Dictionary<Payment, PropertyDescriptorCollection>();

            newProps = new List<string>();

            foreach (var payment in ((JArray)dynamicObject.payments).ToArray<dynamic>())
            {
                var objPayment = new Payment();

                using var propertyManager = new DynamicPropertyManager<Payment>();
                var properties = TypeDescriptor.GetProperties(typeof(Payment));

                foreach (JProperty prop in payment)
                {
                    prop.SetPropertyValue(properties, propertyManager, objPayment, modelsPath, namespaceName, newProps);
                }

                objPayments.Add(objPayment, TypeDescriptor.GetProperties(typeof(Payment)));
            }

            return objPayments;
        }
    }
}
