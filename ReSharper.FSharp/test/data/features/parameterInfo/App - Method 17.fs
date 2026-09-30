type Test() =
    member this.M(a: string) = ()
    member this.M(a: uint32) = ()
    member this.M(a: string, b: int) = ()
    member this.M(a: uint32, b: int) = ()
    member this.M(a: string, b: double) = ()
    member this.M(a: uint32, b: int, c: int) = ()

let t = Test()
t.M({caret})
