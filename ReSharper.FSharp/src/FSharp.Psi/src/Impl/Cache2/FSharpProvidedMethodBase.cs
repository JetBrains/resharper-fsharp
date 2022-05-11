using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public abstract class FSharpProvidedMethodBase<T> : FSharpProvidedMember<T>, IFunction
    where T : ProvidedMethodBase
  {
    protected FSharpProvidedMethodBase(T info, ITypeElement containingType) : base(info, containingType)
    {
    }

    public abstract override DeclaredElementType GetElementType();

    public InvocableSignature GetSignature(ISubstitution substitution) => new(this, substitution);

    public IEnumerable<IParametersOwnerDeclaration> GetParametersOwnerDeclarations() =>
      EmptyList<IParametersOwnerDeclaration>.Instance;

    public IList<IParameter> Parameters => Info
      .GetParameters()
      .Select(t => (IParameter)new FSharpProvidedParameter(t, this))
      .ToList();

    public abstract IType ReturnType { get; }
    public ReferenceKind ReturnKind => ReferenceKind.VALUE;
    public IAttributesSet ReturnTypeAttributes => EmptyAttributesSet.Instance;
    public bool IsPredefined => false;
    public bool IsIterator => false;
  }
}
