using System.Collections.Generic;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Metadata;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
{
  public class FSharpMetadata
  {
    public readonly Dictionary<string, FSharpMetadataEntity> Entities = new();

    public void AddEntity(FSharpMetadataEntity entity, IMetadataAssembly metadataAssembly)
    {
      var qualifiedName = FSharpMetadataEntityModule.getEntityQualifiedName(entity);
      if (Entities.ContainsKey(qualifiedName))
      {
        Logger.GetLogger<FSharpMetadataReader>().Warn($"Duplicate type definition in {metadataAssembly.AssemblyName}");
        return;
      }

      Entities.Add(qualifiedName, entity);
    }
  }
}
