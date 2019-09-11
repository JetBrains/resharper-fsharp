namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  /// Prevents getting reference from FSharpIdentifierToken until references are refactored to be created by reference tree nodes.
  /// todo: remove it, this is a temporary interface to be used during a refactoring.
  public interface IPreventsChildResolve
  {
  }
}
