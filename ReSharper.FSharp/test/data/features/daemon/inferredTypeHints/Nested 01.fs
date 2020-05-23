let outerLet =
    let myFunc (o: obj) =
        match o with
        | :? int as i ->
            let x =
                let myLambda = fun x (y: int) -> x + y
                let myValue = 123
                myLambda myValue i
        | _ -> 0
        
    myFunc 10
