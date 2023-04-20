using System.Collections.Generic;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Metadata;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
{
  public class FSharpMetadata
  {
    public readonly Dictionary<string, FSharpMetadataEntity> Entities = new();

    public void AddEntity(FSharpMetadataEntity entity)
    {
      var qualifiedName = FSharpMetadataEntityModule.getEntityQualifiedName(entity);
      Assertion.Assert(!Entities.ContainsKey(qualifiedName));
      Entities.Add(qualifiedName, entity);
    }
  }
}
