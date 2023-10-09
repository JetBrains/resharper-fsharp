namespace Test

type AAttribute() =
    inherit System.Attribute()

type RM =
  { Field1: int
    Field2{caret}: int
    Field3: int }
