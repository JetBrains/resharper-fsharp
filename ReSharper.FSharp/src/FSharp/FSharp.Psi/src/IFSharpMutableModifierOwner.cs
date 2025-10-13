namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IFSharpMutableModifierOwner
  {
    bool IsMutable { get; }
    void SetIsMutable(bool value);

    /// Some declared elements created from patterns cannot be made mutable,
    /// e.g. function parameters or patterns in match clauses.
    bool CanBeMutable { get; }
  }
}
