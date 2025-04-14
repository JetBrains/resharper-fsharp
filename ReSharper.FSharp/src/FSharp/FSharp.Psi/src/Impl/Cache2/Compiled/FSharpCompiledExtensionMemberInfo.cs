using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Metadata;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2.ExtensionMethods;
using JetBrains.ReSharper.Psi.Modules;
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
    bool Matches(ITypeMember member)
    {
      if (member.ShortName != value.CompiledName?.Value)
        return false;

      if (value.ApparentEnclosingTypeReference is not FSharpMetadataTypeReference.NonLocal typeRef)
        return false;

      if (member is not IMethod method)
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

    return new HybridCollection<ITypeMember>(owner.GetMembers().Where(Matches));
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

  public ExtensionMemberKind Kind => FSharpExtensionMemberKind.FSharpExtensionMember;

  public bool GetExtendedTypePattern(out CompiledCandidateType candidateType)
  {
    if (value.ApparentEnclosingTypeReference is FSharpMetadataTypeReference.NonLocal typeRef)
    {
      if (typeRef.typeNames.LastOrDefault() is { } name)
      {
        candidateType = new CompiledCandidateType(name.SubstringBeforeLast("`"), false);
        return true;
      }
    }

    candidateType = default;
    return false;
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
