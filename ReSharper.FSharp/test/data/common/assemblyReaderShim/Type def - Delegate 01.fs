module Module

let d1: Delegate1 = Delegate1(fun () -> ())
d1.Invoke()

let d2 = Delegate2(fun (s: string) -> ())
d2.Invoke("")

let d3 = Delegate3(fun (s: string) (i: int) -> ())
d3.Invoke("", 1)
