﻿using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class ModuleFunction : FSharpMethodBase<TopPatternDeclarationBase>, ITopLevelPatternDeclaredElement
  {
    public ModuleFunction([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public override bool IsStatic => true;
  }
}
