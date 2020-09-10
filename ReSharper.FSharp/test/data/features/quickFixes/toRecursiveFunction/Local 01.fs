do
    let hello (name: string) =
        let anotherNestedFunction x =
            {caret}hello x

        printfn "Hello %s" name
    ()
