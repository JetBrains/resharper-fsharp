namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TupleExpr
  {
    public bool IsStruct => StructKeyword != null;
  }
}
