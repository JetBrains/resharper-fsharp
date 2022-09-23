module Module

let a = []
let _ = seq {
    for _ = 1 to 16 do yield! a
}
