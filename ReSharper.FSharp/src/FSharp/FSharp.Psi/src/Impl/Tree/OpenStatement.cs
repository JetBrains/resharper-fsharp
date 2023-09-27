namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class OpenStatement
  {
    public bool IsSystem => ReferenceName.GetFirstName().ShortName == "System";
  }
}
