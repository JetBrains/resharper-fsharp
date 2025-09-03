using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Metadata;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2.ExtensionMethods;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;
using JetBrains.Util.DataStructures;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Compiled;

internal class FSharpCompiledExtensionMemberInfo([NotNull] FSharpMetadataValue value, FSharpCompiledModule owner)
  : ICompiledExtensionMemberProxy
{
  public IPsiModule PsiModule => owner.Module;

  public IPsiSourceFile TryGetSourceFile() => null;

  public HybridCollection<ITypeMember> FindExtensionMember()
  {
    var result = new LocalList<ITypeMember>();
    foreach (var member in owner.GetMembers())
    {
      if (Matches(member))
        result.Add(member);
    }

    return new HybridCollection<ITypeMember>(result.ReadOnlyList());

    bool Matches(ITypeMember member)
    {
      if (member is not IMethod method)
        return false;

      var shortName = method.ShortName;
      if (shortName != value.CompiledName?.Value)
        return false;

      if (shortName.EndsWith(".Static", StringComparison.OrdinalIgnoreCase))
        return true;

      if (value.ApparentEnclosingTypeReference is not FSharpMetadataTypeReference.NonLocal typeRef)
        return false;

      var parameters = method.Parameters;
      if (parameters.Count == 0)
        return false;

      // todo: test nested types
      // todo: check full name
      if (parameters[0].Type is IDeclaredType declaredType && declaredType.GetTypeElement() is { } typeElement)
        return typeElement.GetSourceName() == typeRef.ShortName?.Value;

      return false;
    }
  }

  public string ShortName
  {
    get
    {
      // todo: check IsProperty instead of checking the name
      var name = value.LogicalName;
      if (name.StartsWith("get_", StringComparison.Ordinal) || name.StartsWith("set_", StringComparison.Ordinal))
        return name.Substring(4);
      return name;
    }
  }

  public ExtensionMemberKind Kind => FSharpExtensionMemberKind.INSTANCE;

  public CompiledReceiverTypeDescriptor? TryGetExtensionReceiverType()
  {
    if (value.ApparentEnclosingTypeReference is FSharpMetadataTypeReference.NonLocal typeRef)
    {
      if (typeRef.typeNames.LastOrDefault() is { } name)
      {
        return new CompiledReceiverTypeDescriptor(name.SubstringBeforeLast("`"), isArray: false);
      }
    }

    return null;
  }
}

public class FSharpSourceExtensionsMembersIndex : SourceExtensionMembersIndex
{
  protected override void CollectPossibleNames(ITypeElement typeElement, List<string> consumer)
  {
    var shortName = typeElement.ShortName;
    consumer.Add(shortName);

    if (typeElement.GetSourceName() is { } sourceName && sourceName != shortName)
      consumer.Add(sourceName);
  }
}
