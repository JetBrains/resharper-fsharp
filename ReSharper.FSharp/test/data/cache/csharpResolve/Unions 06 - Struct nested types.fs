module Module

[<Struct>]
type U =
    | CaseA of int
    | CaseB of Named: int
    | CaseC of int * Other: float
