using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using JetBrains.Application.BuildScript;
using JetBrains.Application.BuildScript.Compile;
using JetBrains.Application.BuildScript.Solution;
using JetBrains.Build;
using JetBrains.Lifetimes;
using JetBrains.Util;
using JetBrains.Util.Special;
using JetBrains.Util.Storage;
using JetBrains.Util.Storage.Packages;

namespace JetBrains.ReSharper.Plugins.FSharp.BuildScript
{
  /// <summary>
  /// XmlDoc of F# Core DLL from its package reference isn't installed with the products when we're not installing XmlDocs in production builds, but “It's needed for tooltips to work properly in some cases” © eugene.auduchinok.
  /// Fetch it from the referenced package and ship with our package as a non-xml-doc content file.
  /// Even if all XmlDocs are being installed, they'd match by content and there will be no error.
  /// </summary>
  public class PackageFsharpCoreXmlDoc
  {
    public static readonly string AssemblySimpleName = "FSharp.Core";

    [BuildStep]
    public static async Task<ImmutableArray<SubplatformFileForPackaging>> Run(AllAssembliesOnEverything allass, PackageReferencesBuildHelper references, Lifetime lifetime, ILogger logger)
    {
      if(allass.FindSubplatformByClass<PackageFsharpCoreXmlDoc>() is not SubplatformOnSources subplatform)
        return ImmutableArray<SubplatformFileForPackaging>.Empty;

      // Find package
      // * by name
      // * we're referencing
      IJetNugetPackage nupkg = await references.GetPackageByNameAsync(lifetime, AssemblySimpleName, $"{AssemblySimpleName} reference for F# XmlDoc", logger);

      // Doc file
      string nameOfXmlDoc = AssemblySimpleName + ExtensionConstants.Xml;
      ImmutableFileItem fiXmlDoc = nupkg.Package.GetContentItemsInDefaultTfx().Where(fi => string.Equals(fi.RelativePath.Name, nameOfXmlDoc, StringComparison.OrdinalIgnoreCase)).OrderBy().SingleOrFirstErr("Cannot find the XmlDoc file {0} in package {1} we're referencing.", nameOfXmlDoc, nupkg.Identity);

      // Put home
      return ImmutableArray.Create(new SubplatformFileForPackaging(subplatform.Name, fiXmlDoc.WithRelativePath(rel => rel.Name), role: PackageFileRole.Other));
    }
  }
}
