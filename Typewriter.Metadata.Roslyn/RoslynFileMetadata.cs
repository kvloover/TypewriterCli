using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Typewriter.Configuration;
using Typewriter.Metadata.Interfaces;

namespace Typewriter.Metadata.Roslyn
{
    public class RoslynFileMetadata : IFileMetadata
    {
        private readonly string _codePath;

        private readonly Action<string[]> _requestRender;
        //private Document _document;
        private SyntaxNode _root;
        private SemanticModel _semanticModel;

        public RoslynFileMetadata(Document document, Settings settings, Action<string[]> requestRender)
        {
            _requestRender = requestRender;
            _codePath = document.FilePath;
            Settings = settings;
            LoadDocument(document);
        }
        public RoslynFileMetadata(string codePath, Settings settings, Action<string[]> requestRender)
        {
            _codePath = codePath;
            _requestRender = requestRender;
            
            Settings = settings;
            LoadFromFile();
        }
        

        public Settings Settings { get; }
        public string Name => System.IO.Path.GetFileName(_codePath);
        public string FullName => _codePath;

        public IEnumerable<IClassMetadata> Classes => RoslynClassMetadata.FromNamedTypeSymbols(GetNamespaceChildNodes<ClassDeclarationSyntax>(), this);
        public IClassMetadata BaseClass => RoslynClassMetadata.FromNamedTypeSymbol(GetNamespaceChildNodes<BaseArgumentListSyntax>().FirstOrDefault());
        public IEnumerable<IDelegateMetadata> Delegates => RoslynDelegateMetadata.FromNamedTypeSymbols(GetNamespaceChildNodes<DelegateDeclarationSyntax>());
        public IEnumerable<IEnumMetadata> Enums => RoslynEnumMetadata.FromNamedTypeSymbols(GetNamespaceChildNodes<EnumDeclarationSyntax>());
        public IEnumerable<IInterfaceMetadata> Interfaces => RoslynInterfaceMetadata.FromNamedTypeSymbols(GetNamespaceChildNodes<InterfaceDeclarationSyntax>(), this);

        private void LoadDocument(Document document)
        {
            //_document = document;
            _semanticModel = document.GetSemanticModelAsync().Result;
            _root = _semanticModel.SyntaxTree.GetRoot();
        }
        private void LoadFromFile()
        {
            if (!System.IO.File.Exists(_codePath)) 
                throw new Exception($"File not found {_codePath}");
            
            var code = System.IO.File.ReadAllText(_codePath);
            
            var tree = CSharpSyntaxTree.ParseText(code);

            List<string> usings = new List<string>()
            {
                "System"
            };

            var metaReferences = Settings.IncludedProjects.Select(p => MetadataReference.CreateFromFile(p)).ToList();
            // metaReferences.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

            CSharpCompilationOptions options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, usings: usings);
            CSharpCompilation compilation = CSharpCompilation.Create("output",new []{tree} , metaReferences, options);
            // var diagnostics = compilation.GetDiagnostics();
            _semanticModel = compilation.GetSemanticModel(tree);
            _root = _semanticModel.SyntaxTree.GetRoot();
        }

        private IEnumerable<INamedTypeSymbol> GetNamespaceChildNodes<T>() where T : SyntaxNode
        {
            var syntaxNodes = _root.ChildNodes().OfType<NamespaceDeclarationSyntax>().SelectMany(n => n.ChildNodes().OfType<T>());
            var ofType = _root.ChildNodes().OfType<T>();
            var symbols = ofType.Concat(
                syntaxNodes)
                .Select(c => _semanticModel.GetDeclaredSymbol(c) as INamedTypeSymbol);

            if (Settings.PartialRenderingMode == PartialRenderingMode.Combined)
            {
                return symbols.Where(s =>
                {
                    var locationToRender = s?.Locations.Select(l => l.SourceTree.FilePath).OrderBy(f => f).FirstOrDefault();
                    if (string.Equals(locationToRender, FullName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    else
                    {
                        if (locationToRender != null)
                            _requestRender?.Invoke(new[] { locationToRender });

                        return false;
                    }
                }).ToList();
            }

            return symbols;
        }
    }
}
