module Module

[<Struct>]
type U =
    private
        | CaseA of int
        | CaseB of Named: int
        | CaseC of int * Other: float
    static member Prop = 123