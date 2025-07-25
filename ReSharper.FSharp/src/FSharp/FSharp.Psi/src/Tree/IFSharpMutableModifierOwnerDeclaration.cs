namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

public interface IFSharpMutableModifierOwnerDeclaration
{
  bool IsMutable { get; }
  void SetIsMutable(bool value);
}
