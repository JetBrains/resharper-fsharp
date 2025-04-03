using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.DeclaredElements;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpMethodBase<TDeclaration> : FSharpTypeParametersOwnerBase<TDeclaration>, IMethod
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    protected FSharpMethodBase([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.METHOD;

    public bool IsExtensionMethod => ExtensionMemberKind == ExtensionMemberKind.CLASSIC_METHOD;

    public override ExtensionMemberKind ExtensionMemberKind => Attributes.HasAttributeInstance(PredefinedType.EXTENSION_ATTRIBUTE_CLASS) ? ExtensionMemberKind.CLASSIC_METHOD : ExtensionMemberKind.NONE;

    public bool IsAsync => false;
    public bool IsVarArg => false;

    public override ISubstitution IdSubstitution => MethodIdSubstitution.Create(this);
  }
}
