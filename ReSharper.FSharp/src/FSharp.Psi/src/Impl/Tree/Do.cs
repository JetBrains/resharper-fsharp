namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class DoStatement
  {
    public bool IsImplicit => DoKeyword == null;
  }
}
