namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public enum FSharpPartKind : byte
  {
    QualifiedNamespace = 0,
    DeclaredNamespace = 1,
    TopLevelModule = 2,
    NestedModule = 3,
    Exception = 4,
    Enum = 5,
    Record = 6,
    Union = 7,
    UnionCase = 8,
    HiddenType = 9,
    Interface = 10,
    Class = 11,
    Struct = 12,
    StructRecord = 13,
    StructUnion = 14,
  }
}