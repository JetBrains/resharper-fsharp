namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class PatternParameterDeclarationGroup
  {
    public bool HasParens => LeftParen != null && RightParen != null;
  }
}
