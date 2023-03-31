using System;

namespace TypewriterCli
{
    public class CliArgs
    {
        public bool GenerateIndex { get; }
        public string TemplatePath { get; }
        public string SourcePath { get; }
        public string ReferencePath { get; }
        public string Regex { get; }
        public bool Recursive { get; }

        public CliArgs(string templatePath,
            string sourcePath,
            string referencePath,
            string regex,
            bool recursive,
            bool generateIndex)
        {
            if (string.IsNullOrEmpty(templatePath))
                throw new ArgumentNullException(nameof(templatePath));

            if (string.IsNullOrEmpty(sourcePath))
                throw new ArgumentNullException(nameof(sourcePath));

            TemplatePath = templatePath;
            SourcePath = sourcePath;
            ReferencePath = referencePath;
            Regex = regex;
            Recursive = recursive;
            GenerateIndex = generateIndex;
        }
    }
}