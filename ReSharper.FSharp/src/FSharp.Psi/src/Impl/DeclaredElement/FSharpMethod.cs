using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpMethod<TDeclaration> : FSharpMethodBase<TDeclaration>
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    public FSharpMethod([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }
  }

  internal class FSharpTypePrivateMethod : FSharpMethodBase<TopPatternDeclarationBase>
  {
    public FSharpTypePrivateMethod([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public override AccessRights GetAccessRights() => AccessRights.INTERNAL;
    public override bool IsStatic => false;
  }
}
