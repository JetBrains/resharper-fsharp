namespace Test

type AAttribute() =
    inherit System.Attribute()

type RM =
  { Field1: int
    [<A>] mutable
        Field2: int
    Field3: int }
