namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  /// Prevents getting references from FSharpIdentifierToken until all references are created by reference tree nodes.
  /// todo: This is a temporary interface to be used during a big refactoring, remove it.
  public interface IPreventsChildResolve
  {
  }
}
