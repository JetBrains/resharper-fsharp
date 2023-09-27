namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public interface IAccessorsNamesClauseOwner
  {
    IAccessorsNamesClause AccessorsClause { get; }
    IAccessorsNamesClause SetAccessorsClause(IAccessorsNamesClause param);
  }
}
