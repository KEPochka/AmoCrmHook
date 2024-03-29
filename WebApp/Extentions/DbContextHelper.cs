﻿using Microsoft.EntityFrameworkCore;

namespace WebApp.Extentions
{
    public static class DbContextHelper
    {
        public static bool AddOrUpdate(this DbContext context, object data)
        {
            var type = data.GetType();
            var properties = type.GetProperties();

            var keyField = properties.Select(p => new { prop = p, keyAttr = p.Name == "Id" })
                                     .Select(x => x.prop)
                                     .FirstOrDefault();
            if (keyField == null)
                throw new InvalidDataException($"{type.FullName} does not have the 'Id' field. Unable to exec AddOrUpdate call.");

            foreach (var property in properties.Where(p => p.Name == p.PropertyType.Name))
            {
                var val = property.GetValue(data);
                if (val != null)
                {
                    if(!AddOrUpdate(context, val))
                        property.SetValue(data, null);
                }
            }

            var keyVal = keyField.GetValue(data);

            var dbEntity = context.Find(type, keyVal);
            if (dbEntity != null)
            {
                var contextEntry = context.Entry(dbEntity);
                if(contextEntry.State == EntityState.Added)
                    return true;

                contextEntry.CurrentValues.SetValues(data);
                contextEntry.State = EntityState.Modified;
                return false;
            }

            context.Add(data);
            return true;
        }
    }
}
