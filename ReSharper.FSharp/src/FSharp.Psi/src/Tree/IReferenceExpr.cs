using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IReferenceExpr : IReferenceExpression
  {
    FSharpIdentifierToken Identifier { get; }

    [NotNull] string ShortName { get; }

    /// Workaround for pseudo-resolve during parts creation needing to look at qualified names like
    /// CompilationRepresentationFlags.ModuleSuffix. 
    [CanBeNull] string QualifiedName { get; }
  }
}
