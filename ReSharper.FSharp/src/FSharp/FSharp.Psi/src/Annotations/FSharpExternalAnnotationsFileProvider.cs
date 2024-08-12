using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Application;
using JetBrains.Metadata.Utils;
using JetBrains.ReSharper.Psi.ExtensionsAPI.ExternalAnnotations;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Annotations
{
  // TODO: fix for application packages detection in the platform
  [ShellComponent]
  public class FSharpExternalAnnotationsFileProvider : IExternalAnnotationsFileProvider
  {
    private readonly OneToSetMap<string, VirtualFileSystemPath> myAnnotations;

    public FSharpExternalAnnotationsFileProvider()
    {
      myAnnotations = new OneToSetMap<string, VirtualFileSystemPath>(StringComparer.OrdinalIgnoreCase);

      var executingFolder = Assembly.GetExecutingAssembly().TryGetPath().ToVirtualFileSystemPath().Parent;
      var annotationsFolder = executingFolder / "Extensions" / "com.jetbrains.rider.fsharp" / "annotations";
      if (!annotationsFolder.ExistsDirectory) return;

      foreach (var file in annotationsFolder.GetChildFiles()) myAnnotations.Add(file.NameWithoutExtension, file);
    }

    public IEnumerable<VirtualFileSystemPath> GetAnnotationsFiles(AssemblyNameInfo assemblyName = null,
      VirtualFileSystemPath assemblyLocation = null)
    {
      return assemblyName == null ? myAnnotations.Values : myAnnotations[assemblyName.Name];
    }
  }
}
