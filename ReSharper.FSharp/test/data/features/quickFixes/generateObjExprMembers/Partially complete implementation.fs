module Module

type I =
  abstract P1: int
  abstract P2: int
  abstract P3: int

let foo = { new I{caret} with
            member this.P1 = failwith "todo" }