type SimpleRecord = { Value : int }
type SimpleDU = Value of int | Value2 of int * int

type DUDummy = SimpleDU
type RecordDummy = SimpleRecord
type StructDummy = Int32
type InterfaceDummy = IDisposable
type ObjectDummy = Object