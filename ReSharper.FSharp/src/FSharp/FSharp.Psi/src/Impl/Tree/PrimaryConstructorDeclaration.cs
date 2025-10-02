using System.Collections.Generic;
using FSharp.Compiler.Symbols;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class PrimaryConstructorDeclaration
  {
    public override TreeTextRange GetNameRange() =>
      GetContainingTypeDeclaration()?.GetNameRange() ?? TreeTextRange.InvalidRange;

    protected override IDeclaredElement CreateDeclaredElement() =>
      new FSharpPrimaryConstructor(this);

    TreeNodeEnumerable<IParametersPatternDeclaration> IParameterOwnerMemberDeclaration.
      ParametersDeclarationsEnumerable => new([ParametersDeclaration]);

    TreeNodeCollection<IParametersPatternDeclaration> IParameterOwnerMemberDeclaration.
      ParametersDeclarations => new([ParametersDeclaration]);

    public ITokenNode EqualsToken =>
      FSharpTypeDeclarationNavigator.GetByPrimaryConstructorDeclaration(this)?.EqualsToken;

    TreeNodeCollection<IFSharpPattern> IParameterOwnerMemberDeclaration.ParameterPatterns => new([ParameterPatterns]);

    // todo: unify interface in the grammar
    public IFSharpParameterDeclaration GetParameterDeclaration(FSharpParameterIndex index) =>
      ((IParameterOwnerMemberDeclaration)this).ParameterPatterns.GetParameterDeclaration(index);

    public IList<IList<IFSharpParameterDeclaration>> GetParameterDeclarations() =>
      ((IParameterOwnerMemberDeclaration)this).ParameterPatterns.GetParameterDeclarations();
    
    public void SetParameterFcsType(FSharpParameterIndex index, FSharpType fcsType) =>
      ((IParameterOwnerMemberDeclaration)this).ParameterPatterns.SetParameterFcsType(this, index, fcsType);
  }
}
