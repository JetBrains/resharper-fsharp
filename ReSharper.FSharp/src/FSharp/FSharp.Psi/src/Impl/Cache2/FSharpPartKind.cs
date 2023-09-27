namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public enum FSharpPartKind : byte
  {
    QualifiedNamespace = 0,
    DeclaredNamespace = 1,

    NamedModule = 2,
    NestedModule = 3,
    AnonModule = 4,

    Exception = 5,
    Enum = 6,
    Record = 7,
    StructRecord = 8,
    Union = 9,
    StructUnion = 10,
    UnionCase = 11,

    Interface = 12,
    Class = 13,
    Struct = 14,
    ClassExtension = 15,
    StructExtension = 16,
    Delegate = 17,

    ObjectExpression = 18,

    AbbreviationOrSingleCaseUnion = 19,
    StructAbbreviationOrSingleCaseUnion = 20,

    HiddenType = 100
  }
}
