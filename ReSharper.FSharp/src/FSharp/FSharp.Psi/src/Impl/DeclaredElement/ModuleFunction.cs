using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;

internal class ModuleFunction([NotNull] ITypeMemberDeclaration declaration)
  : FSharpMethodBase<TopPatternDeclarationBase>(declaration), ITopLevelPatternDeclaredElement
{
  public override bool IsStatic => true;

  public override DeclaredElementType FSharpElementType => FSharpDeclaredElementType.Function;
}
