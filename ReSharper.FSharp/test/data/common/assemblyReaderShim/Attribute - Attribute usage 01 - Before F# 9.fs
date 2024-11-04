module Module

[<CustomClass; CustomStruct; CustomMethod>]
type C1() =
    [<CustomMethod>]
    member x.M() = ()

[<CustomClass>]
type C2() =
    class end

[<CustomStruct>]
type C3 =
    struct end
