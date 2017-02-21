namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal enum FSharpSerializationTag : byte
  {
    QualifiedNamespacePart = 0,
    DeclaredNamespacePart = 1,
    ModulePart = 2,
    NestedModulePart = 3,
    ExceptionPart = 4,
    EnumPart = 5,
    RecordPart = 6,
    UnionPart = 7,
    TypedUnionCasePart = 8,
    TypeAbbreviationPart = 9,
    InterfacePart = 10,
    ClassPart = 11,
    StructPart = 12,
    OtherSimpleTypePart = 20
  }
}