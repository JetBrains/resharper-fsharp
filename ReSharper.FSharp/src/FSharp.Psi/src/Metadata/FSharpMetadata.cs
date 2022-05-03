using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Metadata;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
{
  public class FSharpMetadata
  {
    public readonly Dictionary<string, Module> Modules = new();

    public void CreateModule(MetadataEntity entity)
    {
      var qualifiedName = GetEntityQualifiedName(entity.CompilationPath, entity.LogicalName, null);

      if (Modules.ContainsKey(qualifiedName))
        return;

      var fsName = FSharpMetadataUtil.GetCompiledModuleDeclaredName(entity.EntityKind, entity.LogicalName);
      var module = new Module(fsName, entity.EntityKind == EntityKind.ModuleWithSuffix);
      Modules.Add(qualifiedName, module);
    }

    public static string GetEntityQualifiedName(Tuple<string, EntityKind>[] compilationPath, string logicalName,
      FSharpOption<string> compiledName)
    {
      var stringBuilder = new StringBuilder(compilationPath.Length * 2 + 1);
      foreach (var (name, entityKind) in compilationPath)
      {
        stringBuilder.Append(name);
        stringBuilder.Append(entityKind == EntityKind.Namespace ? "." : "+");
      }

      stringBuilder.Append(compiledName?.Value ?? logicalName);
      return stringBuilder.ToString();
    }

    public class Module
    {
      [NotNull] public readonly FSharpDeclaredName FSharpName;
      public bool HasModuleSuffix;

      public Module([NotNull] FSharpDeclaredName fsName, bool hasModuleSuffix)
      {
        FSharpName = fsName;
        HasModuleSuffix = hasModuleSuffix;
      }
    }
  }

  public class MetadataEntity
  {
    public int Index;

    public string LogicalName;
    public EntityKind EntityKind { get; set; }
    public Tuple<string, EntityKind>[] CompilationPath { get; set; }
  }
}
