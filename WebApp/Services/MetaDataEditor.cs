using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using WebApp.DataContext;
using WebApp.Extentions;

namespace WebApp.Services
{
    public interface IMetaDataEditor
    {
        void CodeFirst(ApplicationDbContext dbContext);
    }

    public class MetaDataEditor : IMetaDataEditor
    {
        private bool _isLoaded;
        private readonly object _lockObj = new();

        private readonly Dictionary<IEntityType, IEnumerable<IProperty>> _notFoudModels = new();
        private readonly Dictionary<IEntityType, IEnumerable<IProperty>> _notFoudProperties = new();

        public void CodeFirst(ApplicationDbContext dbContext)
        {
            Load(dbContext);

            foreach (var info in _notFoudModels)
            {
                var constraints = new List<string>();

                var table = info.Key.GetTableName();
                var cmdString = new StringBuilder();
                cmdString.AppendLine($"CREATE TABLE {table} (");
                cmdString.AppendLine("id bigserial NOT NULL,");

                foreach (var column in info.Value.Where(x => x.GetColumnName() != "id"))
                    column.AddColumn(cmdString, constraints);
                
                foreach (var constraint in constraints)
                    cmdString.AppendLine(constraint);

                cmdString.AppendLine($"CONSTRAINT {table}_pk PRIMARY KEY (id));");

                dbContext.Database.ExecuteSqlRaw(cmdString.ToString());
            }

            foreach (var info in _notFoudProperties)
            {
                var constraints = new List<string>();

                var table = info.Key.GetTableName();
                var cmdString = new StringBuilder();
                cmdString.AppendLine($"ALTER TABLE {table}");

                foreach (var column in info.Value.Where(x => x.GetColumnName() != "id"))
                    cmdString.AppendLine("ADD COLUMN " + column.AddColumn(constraints));

                foreach (var constraint in constraints)
                    cmdString.AppendLine(constraint);

                cmdString[^3] = ';';

                dbContext.Database.ExecuteSqlRaw(cmdString.ToString());
            }
        }

        private void Load(ApplicationDbContext dbContext)
        {
            _isLoaded = false;

            lock (_lockObj)
            {
                if (_isLoaded)
                    return;

                try
                {
                    _notFoudModels.Clear();
                    _notFoudProperties.Clear();

                    var models = dbContext.Model.GetEntityTypes().ToArray();

                    var tables = dbContext.RawSqlQuery("SELECT table_name FROM information_schema.tables WHERE table_schema='public';", x => x.GetString(0).Underscore());
                    foreach (var table in tables)
                    {
                        var columns = dbContext.RawSqlQuery($"SELECT column_name FROM information_schema.columns WHERE table_schema='public' AND table_name='{table}';", x => x.GetString(0).Underscore());

                        var model = models.FirstOrDefault(x => x.GetTableName() == table);
                        if (model != null)
                        {
                            var properties = model.GetProperties().ToArray();
                            var fndProps = properties.Where(x => columns.Contains(x.Name.Underscore())).ToArray();
                            if (fndProps.Length < properties.Length)
                                _notFoudProperties.Add(model, properties.Where(x => !columns.Contains(x.Name.Underscore())).ToArray());
                        }
                    }
                    foreach (var model in models)
                    {
                        if (tables.All(x => model.GetTableName() != x))
                            _notFoudModels.Add(model, model.GetProperties().ToArray());
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                    throw;
                }
                _isLoaded = true;
            }
        }
    }
}
