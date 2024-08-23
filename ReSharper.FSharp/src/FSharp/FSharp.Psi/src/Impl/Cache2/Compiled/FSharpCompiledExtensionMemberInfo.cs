using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Metadata;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2.ExtensionMethods;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util.DataStructures;

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
      if (value.ApparentEnclosingTypeReference is not FSharpMetadataTypeReference.NonLocal typeRef)
        return false;

      if (member is not IMethod method)
        return false;

      var parameters = method.Parameters;
      if (parameters.Count == 0)
        return false;

      if (parameters[0].Type is IDeclaredType declaredType)
        return declaredType.GetClrName().ShortName == typeRef.ShortName?.Value;

      return false;
    }

    return new HybridCollection<ITypeMember>(owner.EnumerateMembers(value.LogicalName, true).Where(Matches));
  }

  public string ShortName => value.LogicalName;
  public ExtensionMemberKind Kind => FSharpExtensionMemberKind.FSharpExtensionMember;

  public bool GetExtendedTypePattern(out CompiledCandidateType candidateType)
  {
    if (value.ApparentEnclosingTypeReference is FSharpMetadataTypeReference.NonLocal typeRef)
    {
      if (typeRef.typeNames.LastOrDefault() is { } name)
      {
        candidateType = new CompiledCandidateType(name, false);
        return true;
      }
    }

    candidateType = default;
    return false;
  }
}
