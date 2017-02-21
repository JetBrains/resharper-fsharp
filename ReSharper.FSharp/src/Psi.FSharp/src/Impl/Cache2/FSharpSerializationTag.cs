namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal enum FSharpSerializationTag : byte
  {
    QualifiedNamespacePart = 0,
    DeclaredNamespacePart = 1,
    ModulePart = 2,
    ExceptionPart = 3,
    RecordPart = 4,
    EnumPart = 5,
    UnionPart = 6,
    TypedUnionCasePart = 7,
    TypeAbbreviationPart = 8,
    InterfacePart = 9,
    ClassPart = 10,
    StructPart = 11,
    OtherSimpleTypePart = 20
  }
}