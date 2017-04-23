using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement
{
  internal class ModuleFunction : FSharpMethodBase<Let>
  {
    public ModuleFunction([NotNull] ITypeMemberDeclaration declaration, [CanBeNull] FSharpMemberOrFunctionOrValue mfv,
      [CanBeNull] IFSharpTypeDeclaration typeDeclaration) : base(declaration, mfv, typeDeclaration)
    {
    }
  }
}