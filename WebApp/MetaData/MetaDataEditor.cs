using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Globalization;
using WebApp.DataContext;
using WebApp.Extentions;

namespace WebApp.DataLoader
{
    public interface IMetaDataEditor
    {
        void CodeFirst(ApplicationDbContext dbContext);
    }

    public class MetaDataEditor : IMetaDataEditor
    {
        private bool _isLoaded = false;
        private object _lockObj = new object();

        private readonly Dictionary<IEntityType, IEnumerable<IProperty>> _notFoudModels = new Dictionary<IEntityType, IEnumerable<IProperty>>();
        private readonly Dictionary<IEntityType, IEnumerable<IProperty>> _notFoudProperties = new Dictionary<IEntityType, IEnumerable<IProperty>>();
        private readonly Dictionary<IEntityType, IEnumerable<string>> _notFoudFields = new Dictionary<IEntityType, IEnumerable<string>>();
        private readonly Dictionary<string, IEnumerable<string>> _notFoudTables = new Dictionary<string, IEnumerable<string>>();

        public MetaDataEditor()
        {
        }

        public void CodeFirst(ApplicationDbContext dbContext)
        {
            Load(dbContext);

            foreach (var info in _notFoudModels)
            {
                var table = info.Key.GetTableName();
                // ALTER TABLE
            }

            foreach (var info in _notFoudProperties)
            {
                var table = info.Key.GetTableName();
                // CREATE TABLE
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
                    _notFoudProperties.Clear();
                    _notFoudTables.Clear();
                    _notFoudFields.Clear();

                    IProperty[]? properties;
                    var textinfo = new CultureInfo("en-US", false).TextInfo;

                    var models = dbContext.Model.GetEntityTypes();

                    var tables = dbContext.RawSqlQuery($"SELECT table_name FROM information_schema.tables WHERE table_schema='public';", x => x.GetString(0).Underscore());
                    foreach (var table in tables)
                    {
                        var columns = dbContext.RawSqlQuery($"SELECT column_name FROM information_schema.columns WHERE table_schema='public' AND table_name='{table}';", x => x.GetString(0).Underscore());

                        var model = models.FirstOrDefault(x => x.GetTableName() == table);
                        if (model != null)
                        {
                            properties = model.GetProperties().ToArray();
                            var fndProps = properties.Where(x => columns.Contains(x.Name.Underscore())).ToArray();
                            if (fndProps.Length < properties.Length)
                                _notFoudProperties.Add(model, properties.Where(x => !columns.Contains(x.Name.Underscore())).ToArray());
                            var notFndCols = columns.Where(x => !properties.Any(p => p.Name.Underscore() == x)).ToArray();
                            if (notFndCols.Length > 0)
                                _notFoudFields.Add(model, notFndCols);
                        }
                        else
                            _notFoudTables.Add(table, columns);
                    }
                    foreach (var model in models)
                    {
                        if (!tables.Any(x => model.GetTableName() == x))
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
