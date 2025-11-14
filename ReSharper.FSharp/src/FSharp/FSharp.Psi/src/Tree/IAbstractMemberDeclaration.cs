namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IAbstractMemberDeclaration : IFSharpParameterOwnerDeclaration
  {
    bool HasDefaultImplementation { get; }
  }
}
