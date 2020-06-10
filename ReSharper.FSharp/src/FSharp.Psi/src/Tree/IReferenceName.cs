using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IReferenceName : IFSharpQualifiableReferenceOwner, ITypeArgumentOwner
  {
    [NotNull] string ShortName { get; }
    [NotNull] string QualifiedName { get; }
  }
}
