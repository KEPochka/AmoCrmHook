using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace WebApp.Extentions
{
    public static class ColumnBuilderHelper
    {
        public static void AddColumn(this IProperty column, StringBuilder cmdString, List<string> constraints)
        {
            cmdString.Append($"\"{column.GetColumnName()}\" {column.GetColumnType()}");
            if (column.ClrType.IsValueType)
            {
                var type = Nullable.GetUnderlyingType(column.ClrType);
                cmdString.AppendLine(type == null ? " NOT NULL," : ",");
            }
            else
                cmdString.AppendLine(",");

            if (column.IsForeignKey())
            {
                constraints.AddRange(column.GetContainingForeignKeys().Select(foreignKey =>
                    $"CONSTRAINT {foreignKey.DeclaringEntityType.GetTableName()}_{foreignKey.PrincipalEntityType.GetTableName()}_fk FOREIGN KEY ({column.GetColumnName()}) REFERENCES {foreignKey.PrincipalEntityType.GetTableName()} ({foreignKey.PrincipalKey.Properties.First().GetColumnName()}),"));
            }
        }

        public static string AddColumn(this IProperty column, List<string> constraints)
        {
            var result = $"\"{column.GetColumnName()}\" {column.GetColumnType()}";
            if (column.ClrType.IsValueType)
            {
                var type = Nullable.GetUnderlyingType(column.ClrType);
                result += (type == null ? " NOT NULL," : ",");
            }
            else
                result += ',';

            if (column.IsForeignKey())
            {
                constraints.AddRange(column.GetContainingForeignKeys().Select(foreignKey =>
                    $"ADD CONSTRAINT {foreignKey.DeclaringEntityType.GetTableName()}_{foreignKey.PrincipalEntityType.GetTableName()}_fk FOREIGN KEY ({column.GetColumnName()}) REFERENCES {foreignKey.PrincipalEntityType.GetTableName()} ({foreignKey.PrincipalKey.Properties.First().GetColumnName()}),"));
            }

            return result;
        }
    }
}
