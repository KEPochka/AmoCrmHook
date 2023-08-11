using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Data;

namespace WebApp.Extentions
{
    public static class SqlQueryHelper
    {
        public static List<T> RawSqlQuery<T>(this DbContext context, string query, Func<DbDataReader, T> map)
        {
            using var command = context.Database.GetDbConnection().CreateCommand();

            command.CommandText = query;
            command.CommandType = CommandType.Text;

            context.Database.OpenConnection();

            using var result = command.ExecuteReader();

            var entities = new List<T>();

            while (result.Read())
                entities.Add(map(result));

            context.Database.CloseConnection();

            return entities;
        }
    }
}
