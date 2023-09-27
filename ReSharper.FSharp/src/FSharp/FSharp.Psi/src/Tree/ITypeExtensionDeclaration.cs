namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface ITypeExtensionDeclaration : IFSharpQualifiableReferenceOwner
  {
    bool IsTypePartDeclaration { get; }
    bool IsTypeExtensionAllowed { get; }
  }
}
