using System.CodeDom;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace WebApp.DynamicTypeGeneration
{
    public static class GenerateType
    {
        public static string ToUnderscoreCase(this string str)
        {
            return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
        }

        public static CodeCompileUnit CreateTarget(string namespaceName, string name, out CodeTypeDeclaration targetClass)
        {
            targetClass = new CodeTypeDeclaration(name)
            {
                IsClass = true,
                TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed
            };

            var tableName = name.EndsWith("y") ? name.Substring(0, name.Length - 1).ToUnderscoreCase() + "ies" : name.ToUnderscoreCase() + "s";
            var codeArg = new CodeAttributeArgument(new CodePrimitiveExpression(tableName));
            targetClass.CustomAttributes.Add(new CodeAttributeDeclaration("Table", codeArg));

            CodeNamespace globalNamespace = new CodeNamespace();
            globalNamespace.Imports.Add(new CodeNamespaceImport("System"));
            globalNamespace.Imports.Add(new CodeNamespaceImport("System.ComponentModel.DataAnnotations"));
            globalNamespace.Imports.Add(new CodeNamespaceImport("System.ComponentModel.DataAnnotations.Schema"));

            var namespaces = new CodeNamespace(namespaceName);
            namespaces.Types.Add(targetClass);

            var targetUnit = new CodeCompileUnit();
            targetUnit.Namespaces.Add(globalNamespace);
            targetUnit.Namespaces.Add(namespaces);
            return targetUnit;
        }

        public static void AddIdProperty(this CodeTypeDeclaration targetClass)
        {
            StringBuilder asmInfo = new StringBuilder();
            asmInfo.AppendLine("        [Column(\"id\")]");
            asmInfo.AppendLine("        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]");
            asmInfo.AppendLine("        public Int64 Id { get; set; }");

            var newProperty = new CodeSnippetTypeMember
            {
                Text = asmInfo.ToString()
            };

            targetClass.Members.Add(newProperty);
        }

        public static void AddForeignKeyProperty(this CodeTypeDeclaration targetClass, string name, string fkName)
        {
            var propSnippet = new StringBuilder();
            propSnippet.AppendLine($"        [Column(\"{name.ToUnderscoreCase()}\")]");
            propSnippet.AppendLine($"        [ForeignKey(\"{fkName}\")]");
            propSnippet.AppendLine($"        public Int64 {name} {{ get; set; }}");
            propSnippet.AppendLine("");
            propSnippet.AppendLine($"        public {fkName}? {fkName} {{ get; set; }}");

            var newProperty = new CodeSnippetTypeMember
            {
                Text = propSnippet.ToString()
            };

            targetClass.Members.Add(newProperty);
        }

        public static void AddProperty(this CodeTypeDeclaration targetClass, string name, Type type)
        {
            string typeName;
            var realType = Nullable.GetUnderlyingType(type);
            if (realType != null)
                typeName = realType.Name + "?";
            else
                typeName = type.Name;

            StringBuilder asmInfo = new StringBuilder();
            asmInfo.AppendLine($"        [Column(\"{name.ToUnderscoreCase()}\")]");
            asmInfo.AppendLine($"        public {typeName} {name} {{ get; set; }}");

            var newProperty = new CodeSnippetTypeMember
            {
                Text = asmInfo.ToString()
            };

            targetClass.Members.Add(newProperty);
        }

        public static CodeCompileUnit GenerateClass(this PropertyDescriptor[] properties, string modelsPath, string namespaceName, string className)
        {
            var targetUnit = CreateTarget(namespaceName, className, out CodeTypeDeclaration targetClass);

            foreach (var prop in properties.Where(x => x.Name != x.PropertyType.Name && x.PropertyType.Name != "Type"))
            {
                var fkName = prop.Name.Substring(0, prop.Name.Length - 2);
                var foreignKey = properties.FirstOrDefault(x => x.Name == fkName && x.PropertyType.IsClass);
                if (foreignKey != null)
                {
                    targetClass.AddForeignKeyProperty(prop.Name, fkName);

                    if (foreignKey.PropertyType.Name != "Type")
                    {
                        var props = TypeDescriptor.GetProperties(foreignKey.PropertyType).Cast<PropertyDescriptor>().ToArray();
                        GenerateClass(props, modelsPath, namespaceName, fkName);
                    }
                }
                else
                {
                    if (prop.Name == "Id")
                        targetClass.AddIdProperty();
                    else
                        targetClass.AddProperty(prop.Name, prop.PropertyType);
                }
            }

            targetUnit.GenerateCSharpCode(modelsPath + className);
            return targetUnit;
        }
    }
}
