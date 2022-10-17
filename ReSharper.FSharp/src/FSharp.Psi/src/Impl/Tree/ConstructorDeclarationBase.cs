using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal abstract class ConstructorDeclarationBase : FSharpProperTypeMemberDeclarationBase
  {
    protected override string DeclaredElementName =>
      IsStatic ? DeclaredElementConstants.STATIC_CONSTRUCTOR_NAME : DeclaredElementConstants.CONSTRUCTOR_NAME;

    public override string SourceName => DeclaredElementName;

    protected override IDeclaredElement CreateDeclaredElement() =>
      new FSharpSecondaryConstructor(this);

    public override IFSharpIdentifierLikeNode NameIdentifier => null;
    public override TreeTextRange GetNameIdentifierRange() => GetNameRange();

    public IParametersOwner DeclaredParametersOwner => (IParametersOwner)DeclaredElement;


    // public IFSharpParameterDeclaration GetParameter(int group, int index) =>
    //   FSharpImplUtil.GetParameter(this, group, index);

    // public abstract IList<IParametersPatternDeclaration> ParameterPatternsDeclarations { get; }

    // public IParameterDeclaration AddParameterDeclarationBefore(ParameterKind kind, IType parameterType,
    //   string parameterName, IParameterDeclaration anchor) =>
    //   throw new System.NotImplementedException();
    //
    // public IParameterDeclaration AddParameterDeclarationAfter(ParameterKind kind, IType parameterType,
    //   string parameterName, IParameterDeclaration anchor) =>
    //   throw new System.NotImplementedException();
    //
    // public void RemoveParameterDeclaration(int index) =>
    //   throw new System.NotImplementedException();
  }
}
