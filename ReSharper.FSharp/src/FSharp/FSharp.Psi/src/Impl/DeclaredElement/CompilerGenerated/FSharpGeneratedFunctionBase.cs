using System.Collections.Generic;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  public abstract class FSharpGeneratedFunctionBase : FSharpGeneratedMemberBase, IFunction
  {
    public override MemberHidePolicy HidePolicy => MemberHidePolicy.HIDE_BY_SIGNATURE;

    public InvocableSignature GetSignature(ISubstitution substitution) => new(this, substitution);

    public override bool Equals(object obj)
    {
      if (!base.Equals(obj))
        return false;

      if (!(obj is IFunction other))
        return false;

      var signature = GetSignature(IdSubstitution);
      var otherSignature = other.GetSignature(other.IdSubstitution);
      return SignatureComparers.Strict.CompareWithoutName(signature, otherSignature);
    }

    public override int GetHashCode() =>
      ShortName.GetHashCode();

    public IEnumerable<IParametersOwnerDeclaration> GetParametersOwnerDeclarations() =>
      EmptyList<IParametersOwnerDeclaration>.Instance;

    public bool IsPredefined => false;
    public bool IsIterator => false;
    public IAttributesSet ReturnTypeAttributes => EmptyAttributesSet.Instance;
    public ReferenceKind ReturnKind => ReferenceKind.VALUE;

    public abstract IList<IParameter> Parameters { get; }
    public abstract IType ReturnType { get; }
  }
}
