module Module =
  type I =
    abstract P1: int
    abstract P2: int

  type T1() =
   interface I{caret} with
     member x.P1 = 1
  type T2() = class end
