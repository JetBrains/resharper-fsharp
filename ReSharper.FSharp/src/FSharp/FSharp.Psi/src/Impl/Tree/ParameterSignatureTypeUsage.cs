using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;

internal partial class ParameterSignatureTypeUsageStub
{
  public override IFSharpIdentifier NameIdentifier => Identifier;

  public IFSharpParameter FSharpParameter =>
    TryGetFSharpParameterIndex() is { } index &&
    GetOwnerDeclaration() is { DeclaredElement: IFSharpParameterOwner parameterOwner }
      ? new FSharpMethodParameter(parameterOwner, index)
      : null;

  public IFSharpParameterOwnerDeclaration GetOwnerDeclaration() =>
    GetContainingNode<IFSharpParameterOwnerDeclaration>();

  public FSharpParameterIndex? TryGetFSharpParameterIndex()
  {
    // todo: parens?

    var tupleTypeUsage = TupleTypeUsageNavigator.GetByItem(this);
    var isSingleParameterGroup = tupleTypeUsage == null;
    var parameterIndex = !isSingleParameterGroup ? tupleTypeUsage.Items.IndexOf(this) : 0;

    var paramGroupDecl = (ITypeUsage)tupleTypeUsage ?? this;
    var typeUsage = FunctionTypeUsageNavigator.GetByArgumentTypeUsage(paramGroupDecl);
    if (typeUsage == null)
      return null;

    var groupIndex = 0;
    while (FunctionTypeUsageNavigator.GetByReturnTypeUsage(typeUsage) is { } functionTypeUsage)
    {
      groupIndex++;
      typeUsage = functionTypeUsage;
    }

    if (groupIndex == -1)
      return null;

    return MemberSignatureLikeDeclarationNavigator.GetByReturnTypeUsage(typeUsage) != null
      ? new FSharpParameterIndex(groupIndex, isSingleParameterGroup ? null : parameterIndex)
      : null;
  }
}

internal class ParameterSignatureTypeUsage : ParameterSignatureTypeUsageStub
{
  public override ITypeUsage SetTypeUsage(ITypeUsage typeUsage)
  {
    if (TypeUsage != null)
      return base.SetTypeUsage(typeUsage);

    var colon = ModificationUtil.AddChildAfter(Identifier, FSharpTokenType.COLON.CreateTreeElement());
    return ModificationUtil.AddChildAfter(colon, typeUsage);
  }
}
