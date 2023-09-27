using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IReferenceExpr : IFSharpQualifiableReferenceOwner, ITypeArgumentOwner
  {
    [NotNull] string ShortName { get; }

    /// Workaround for pseudo-resolve during parts creation needing to look at qualified names like
    /// CompilationRepresentationFlags.ModuleSuffix.
    [CanBeNull] string QualifiedName { get; }
    
    bool IsSimpleName { get; }
  }
}
