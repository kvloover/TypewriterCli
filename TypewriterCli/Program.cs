using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Mono.Options;
using Typewriter.CodeModel.Configuration;
using Typewriter.CodeModel.Implementation;
using Typewriter.Generation;
using Typewriter.Metadata.Roslyn;


namespace TypewriterCli
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            //var stopwatch = Stopwatch.StartNew();
            var showHelp = args == null || args.Length == 0;

            string templatePath = null;
            string sourcePath = null;
            string referencePath = null;
            string regex = null;
            bool recursive = false;
            bool generateIndex = false;

            var p = new OptionSet
            {
                { "t|template=", "full path to template (*.tst) file.", v => templatePath = v },
                { "s|source=", "full path to source (*.cs) file. or comma seperated directories", v => sourcePath = v },
                { "r|reference=", "full path to assembly to reference", v => referencePath = v },
                { "x|regex=", "regex", v => regex = v },
                { "e|recursive", "recursive", v => recursive = true },
                { "i|index", "should generate index.", v => generateIndex = true },
                { "h|help", "show this message and exit", v => showHelp = v != null }
            };

            try
            {
                p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("TypewriterCli: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `dotnet TypewriterCli.dll --help' for more information.");
                return;
            }

            try
            {
                if (showHelp)
                {
                    ShowHelp(p);
                    return;
                }

                var cliArgs = new CliArgs(templatePath, sourcePath, referencePath, regex, recursive, generateIndex);

                if (cliArgs.TemplatePath == null)
                    throw new InvalidOperationException("Missing required option -t|template");

                if (cliArgs.SourcePath == null)
                    throw new InvalidOperationException("Missing required option -s|source");


                Generate(cliArgs);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
        }

        public static void Generate(CliArgs cliArgs)
        {
            var settings = new SettingsImpl();
            var template = new Template(cliArgs.TemplatePath);
            var provider = new RoslynMetadataProvider();
            var indexBuilder = new StringBuilder();
            if (!string.IsNullOrEmpty(cliArgs.ReferencePath))
                foreach (var path in cliArgs.ReferencePath.Split(","))
                    settings.IncludePath(path);

            // detects whether its a directory or file
            foreach (var path in GetFiles(cliArgs.SourcePath, cliArgs.Recursive, cliArgs.Regex))
            {
                var file = new FileImpl(provider.GetFile(path, settings, null));
                var outputPath = template.RenderFile(file);
                if (outputPath != null)
                {
                    indexBuilder.Append(ExportStatement(outputPath));
                }
            }

            if (cliArgs.GenerateIndex)
            {
                var @join = Path.Join(Path.GetDirectoryName(cliArgs.TemplatePath), "index.ts");
                Console.WriteLine($"Outputting to {@join}");
                File.WriteAllText(@join, indexBuilder.ToString(), new UTF8Encoding(true));
            }
        }

        private static string[] GetFiles(string sourcePaths, bool recursive, string regex)
        {
            List<string> result = new List<string>();
            foreach (var sourcePath in sourcePaths.Split(","))
            {
                FileAttributes attr = File.GetAttributes(sourcePath);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    result.AddRange(recursive ? GetFilesRecursive(sourcePath, regex) : Directory.GetFiles(sourcePath));
                }
                else
                {
                    result.Add(sourcePath);
                }
            }

            return result.ToArray();
        }

        private static IEnumerable<string> GetFilesRecursive(string sourcePath, string regex)
        {
            var result = new List<string>();
            DirSearch(sourcePath, result, regex == null ? null : new Regex(regex));
            return result;
        }

        static void DirSearch(string sDir, List<string> result, Regex wildCard)
        {
            var directories = Directory.GetDirectories(sDir).ToList();
            directories.Add(sDir);
            foreach (string d in directories)
            {
                foreach (string f in Directory.GetFiles(d))
                {
                    if (wildCard == null || wildCard.IsMatch(f))
                    {
                        result.Add(f);
                    }
                }

                if (!d.Equals(sDir))
                {
                    DirSearch(d, result, wildCard);
                }
            }
        }

        private static string ExportStatement(String outputPath)
        {
            return $"export * from \"./{Path.GetFileName(outputPath).Replace(".ts", "")}\";\n";
        }

        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage:  dotnet TypewriterCli.dll [OPTIONS]");
            Console.WriteLine();
            Console.WriteLine(
                "TypewriterCli generates TypeScript files from c# code files using TypeScript Templates.");
            Console.WriteLine(
                "For more information about TypeScript Templates, see here: https://frhagn.github.io/Typewriter/index.html.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }
    }
}