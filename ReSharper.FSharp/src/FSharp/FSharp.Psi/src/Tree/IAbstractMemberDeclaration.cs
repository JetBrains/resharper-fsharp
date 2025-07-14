namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IAbstractMemberDeclaration : IAccessorsNamesClauseOwner, IFSharpParameterOwnerDeclaration
  {
    bool HasDefaultImplementation { get; }
  }
}
