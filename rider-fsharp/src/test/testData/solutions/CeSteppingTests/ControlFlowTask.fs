module ControlFlow.Task

let a1 =
    task {
        let l = [1; 2; 3]

        match l |> List.head with
        | 0 -> printfn "0"
        | 1 when false -> printfn "1"
        | 1 -> printfn "1"
        | _ -> printfn "_"

        printfn "()"
    }

let a2 =
    task {
        let l = [1; 2; 3]

        for i in l do
            printfn $"{i}"

        printfn "()"
    }

let a3 =
    task {
        let l = [1; 2; 3]

        for i in l do
            let j = i + 1
            printfn $"{j}"

        printfn "()"
    }

let run () =
    let computations = [a1; a2; a3]
    for a in computations do
        a.Result |> ignore

run ()
