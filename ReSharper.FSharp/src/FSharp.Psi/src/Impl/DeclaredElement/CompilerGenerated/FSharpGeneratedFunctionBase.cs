using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  public abstract class FSharpGeneratedFunctionBase : FSharpGeneratedMemberBase, IFunction
  {
    public override MemberHidePolicy HidePolicy => MemberHidePolicy.HIDE_BY_SIGNATURE;

    public InvocableSignature GetSignature(ISubstitution substitution) =>
      new InvocableSignature(this, substitution);

    public override bool Equals(object obj)
    {
      if (!base.Equals(obj))
        return false;

      if (!(obj is IFSharpTypeMember && obj is IFunction))
        return false;

      var member = (IFunction) obj;
      var signature = GetSignature(IdSubstitution);
      var memberSignature = member.GetSignature(member.IdSubstitution);
      return SignatureComparers.Strict.CompareWithoutName(signature, memberSignature);
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