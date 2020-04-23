namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class Do
  {
    public bool IsImplicit => DoKeyword == null;
  }
}
