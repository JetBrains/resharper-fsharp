namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IBindingLikeDeclaration : IFSharpParameterOwnerDeclaration, IFSharpTypeOwnerDeclaration
  {
    bool IsMutable { get; }
    void SetIsMutable(bool value);
  }
}
