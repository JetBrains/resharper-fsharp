let run () =
    let x = 15
    let y = failwith ""
    let z = [1;2;3]
    let u = [|1;2;3|]
    let ts = 1, "hi", 10.
    let disp = { new System.IDisposable with override __.Dispose () = () }
    let dispB = disp :> obj
    y + ""
