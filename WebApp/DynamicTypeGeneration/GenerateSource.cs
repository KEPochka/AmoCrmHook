using Microsoft.CSharp;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace WebApp.DynamicTypeGeneration
{
    public static class GenerateSource
    {
        public static string GenerateCSharpCode(this CodeCompileUnit compileunit, string sourceFile)
        {
            using var provider = new CSharpCodeProvider();

            if (provider.FileExtension[0] == '.')
                sourceFile += provider.FileExtension;
            else
                sourceFile = string.Concat(sourceFile, ".", provider.FileExtension);

            var folder = Path.GetDirectoryName(sourceFile);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            using var sw = new StreamWriter(sourceFile, false);
            using var tw = new IndentedTextWriter(sw, "    ");

            provider.GenerateCodeFromCompileUnit(compileunit, tw, new CodeGeneratorOptions());

            tw.Flush();
            tw.Close();
            sw.Close();

            return sourceFile;
        }

        public static bool CompileCSharpCode(string[] sourceFiles, string assemblyFile)
        {
            var assemblyName = Path.GetFileNameWithoutExtension(assemblyFile);

            var asmInfo = new StringBuilder();
            asmInfo.AppendLine("using System;");
            asmInfo.AppendLine("using System.Reflection;");
            asmInfo.AppendLine($"[assembly: AssemblyTitle(\"{assemblyName}\")]");
            asmInfo.AppendLine("[assembly: AssemblyVersion(\"1.0.0.0\")]");
            asmInfo.AppendLine("[assembly: AssemblyFileVersion(\"1.0.0.0\")]");

            asmInfo.AppendLine($"[assembly: AssemblyProduct(\"{assemblyName}\")]");
            asmInfo.AppendLine("[assembly: AssemblyInformationalVersion(\"1.0.0\")]");

            var syntaxTrees = new List<SyntaxTree>
            {
                CSharpSyntaxTree.ParseText(SourceText.From(asmInfo.ToString(), encoding: Encoding.Default), path: Path.GetDirectoryName(assemblyFile) + "\\AssemblyInfo.cs")
            };

            foreach (var sourceFile in sourceFiles)
            {
                using var stream = File.OpenRead(sourceFile);
                var syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(stream), path: sourceFile);
                syntaxTrees.Add(syntaxTree);
            }

            var refPaths = new[]
            {
                typeof(DateTime).GetTypeInfo().Assembly.Location,
                typeof(System.ComponentModel.DataAnnotations.KeyAttribute).GetTypeInfo().Assembly.Location,
                Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location) ?? "", "System.Runtime.dll")
            };
            var references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();

            var compilation = CSharpCompilation.Create(
                Path.GetFileName(assemblyFile),
                syntaxTrees: syntaxTrees,
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var result = compilation.Emit(assemblyFile, Path.GetDirectoryName(assemblyFile) + "\\" + assemblyName + ".pdb");

            if (result.Success)
            {
                Console.Out.WriteLine($"Assembly '{Path.GetFileName(assemblyFile)}' successfully compiled.");
            }
            else
            {
                var failures = result.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

                foreach (var diagnostic in failures)
                    Console.Error.WriteLine("\t{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
            }

            return result.Success;
        }
    }
}
