using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class PrimaryConstructorDeclaration
  {
    public override TreeTextRange GetNameRange() =>
      GetContainingTypeDeclaration()?.GetNameRange() ?? TreeTextRange.InvalidRange;

    protected override IDeclaredElement CreateDeclaredElement() =>
      new FSharpPrimaryConstructor(this);

    public IList<IFSharpParameterDeclarationGroup> ParameterGroups =>
      new[] { (IFSharpParameterDeclarationGroup)ParametersDeclaration };

    public IList<IFSharpParameterDeclaration> ParameterDeclarations => this.GetParameterDeclarations();

    public IFSharpParameterDeclaration GetParameter((int group, int index) position) =>
      FSharpImplUtil.GetParameter(this, position);

    // public override IList<IParametersPatternDeclaration> ParameterPatternsDeclarations =>
    //   this.GetParameterDeclarations();
  }
}
