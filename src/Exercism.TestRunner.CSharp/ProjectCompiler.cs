using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace Exercism.TestRunner.CSharp
{
    internal static class ProjectCompiler
    {
        public static async Task<Compilation> Compile(Options options)
        {
            var workspace = MSBuildWorkspace.Create();
            var project = await workspace.OpenProjectAsync(GetProjectPath(options));

            return await project
                .WithMetadataReferences(GetMetadataReferences())
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .GetCompilationAsync();
        }

        private static string GetProjectPath(Options options) =>
            Path.Combine(options.InputDirectory, $"{options.Slug.Dehumanize().Pascalize()}.csproj");

        private static IEnumerable<PortableExecutableReference> GetMetadataReferences() =>
            AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
                .ToString()
                .Split(":")
                .Select(metadataFilePath => MetadataReference.CreateFromFile(metadataFilePath));
    }
}