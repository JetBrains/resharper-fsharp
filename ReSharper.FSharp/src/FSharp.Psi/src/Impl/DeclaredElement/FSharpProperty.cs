using System.Linq;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpProperty<TDeclaration> : FSharpPropertyBase<TDeclaration>
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    public FSharpProperty([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv) : base(declaration, mfv)
    {
    }

    public override bool IsStatic => GetContainingType() is IFSharpModule || base.IsStatic;
  }
}
