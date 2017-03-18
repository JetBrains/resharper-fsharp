using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement
{
  internal class FSharpImplicitConstructor : FSharpTypeMember<ImplicitConstructorDeclaration>, IConstructor
  {
    public FSharpImplicitConstructor([NotNull] IDeclaration declaration) : base(declaration)
    {
    }

    public override DeclaredElementType GetElementType()
    {
      return CLRDeclaredElementType.CONSTRUCTOR;
    }

    public InvocableSignature GetSignature(ISubstitution substitution)
    {
      return new InvocableSignature(this, substitution);
    }

    public IEnumerable<IParametersOwnerDeclaration> GetParametersOwnerDeclarations()
    {
      return EmptyList<IParametersOwnerDeclaration>.Instance;
    }

    public IList<IParameter> Parameters => EmptyList<IParameter>.Instance;
    public IType ReturnType => Module.GetPredefinedType().Void;
    public bool IsRefReturn => false;
    public bool IsPredefined => false;
    public bool IsIterator => false;
    public IAttributesSet ReturnTypeAttributes => EmptyAttributesSet.Instance; // todo
    public bool IsDefault => false;

    public bool IsParameterless => true; // todo
    public bool IsImplicit => true;
  }
}