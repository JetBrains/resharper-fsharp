using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ConstructorSignature
  {
    public override TreeTextRange GetNameRange() => NewKeyword.GetTreeTextRange();

    protected override IDeclaredElement CreateDeclaredElement() =>
      Parent is IMemberConstraint
        ? null
        : new FSharpSecondaryConstructor(this);

    // public IParameterDeclaration AddParameterDeclarationBefore(ParameterKind kind, IType parameterType,
    //   string parameterName, IParameterDeclaration anchor) =>
    //   throw new NotImplementedException();
    //
    // public IParameterDeclaration AddParameterDeclarationAfter(ParameterKind kind, IType parameterType,
    //   string parameterName, IParameterDeclaration anchor) =>
    //   throw new NotImplementedException();
    //
    // public void RemoveParameterDeclaration(int index) =>
    //   throw new NotImplementedException();

    // public IParametersOwner DeclaredParametersOwner => (IParametersOwner)DeclaredElement;

    public IList<IFSharpParameterDeclarationGroup> ParameterGroups => this.GetParameterGroups();

    public IList<IFSharpParameterDeclaration> ParameterDeclarations => this.GetParameterDeclarations();

    public IFSharpParameterDeclaration GetParameter((int group, int index) position) =>
      FSharpImplUtil.GetParameter(this, position);

    // public override IList<IParametersPatternDeclaration> ParameterPatternsDeclarations { get; }
  }
}
