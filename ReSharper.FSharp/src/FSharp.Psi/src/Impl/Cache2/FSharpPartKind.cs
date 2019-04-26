namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public enum FSharpPartKind : byte
  {
    QualifiedNamespace = 0,
    DeclaredNamespace = 1,

    TopLevelModule = 2,
    NestedModule = 3,
    AnonModule = 4,

    Exception = 5,
    Enum = 6,
    Record = 7,
    Union = 8,
    UnionCase = 9,

    Interface = 10,
    Class = 11,
    Struct = 12,
    StructRecord = 13,
    StructUnion = 14,
    ClassExtension = 15,
    StructExtension = 16,
    Delegate = 17,

    HiddenType = 18,
  }
}
