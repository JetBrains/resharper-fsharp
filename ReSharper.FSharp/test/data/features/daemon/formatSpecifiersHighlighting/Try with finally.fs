module Module

try
    let _ = sprintf "foo %A bar" 0
    try
        printfn "foo %b bar" true
    finally
        printf "foo %i bar" 0
with _ ->
    failwithf "foo %d bar" 0