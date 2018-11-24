using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  public class FSharpGeneratedProperty : FSharpGeneratedMemberBase, IProperty
  {
    public FSharpGeneratedProperty([NotNull] Class containingType, [NotNull] string shortName, IType type,
      bool isStatic = false) : base(containingType)
    {
      ShortName = shortName;
      ReturnType = type;
      IsStatic = isStatic;
    }

    public override DeclaredElementType GetElementType()
    {
      return CLRDeclaredElementType.PROPERTY;
    }

    public override string ShortName { get; }
    public override MemberHidePolicy HidePolicy => MemberHidePolicy.HIDE_BY_NAME;
    public bool IsExplicitImplementation => false;
    public IList<IExplicitImplementation> ExplicitImplementations => EmptyList<IExplicitImplementation>.Instance;
    public bool CanBeImplicitImplementation => false;

    public InvocableSignature GetSignature(ISubstitution substitution)
    {
      return new InvocableSignature(this, substitution);
    }

    public IEnumerable<IParametersOwnerDeclaration> GetParametersOwnerDeclarations()
    {
      return EmptyList<IParametersOwnerDeclaration>.Instance;
    }

    public IList<IParameter> Parameters => EmptyList<IParameter>.Instance;
    public IType ReturnType { get; }
    public IType Type => ReturnType;

    public string GetDefaultPropertyMetadataName()
    {
      return ShortName;
    }

    public IAccessor Getter => new ImplicitAccessor(this, AccessorKind.GETTER);
    public IAccessor Setter => null;
    public bool IsReadable => true;
    public bool IsWritable => false;
    public bool IsAuto => true;
    public bool IsDefault => false;
    public override bool IsStatic { get; }
  }
}