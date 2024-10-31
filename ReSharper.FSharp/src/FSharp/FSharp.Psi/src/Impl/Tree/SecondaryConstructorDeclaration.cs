﻿using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class SecondaryConstructorDeclaration
  {
    public override TreeTextRange GetNameRange() => NewKeyword.GetTreeTextRange();

    protected override IDeclaredElement CreateDeclaredElement() =>
      new FSharpSecondaryConstructor(this);

    TreeNodeEnumerable<IParametersPatternDeclaration> IParameterOwnerMemberDeclaration.
      ParametersDeclarationsEnumerable => new([ParametersDeclaration]);

    TreeNodeCollection<IFSharpPattern> IParameterOwnerMemberDeclaration.ParameterPatterns => new([ParameterPatterns]);
  }
}
