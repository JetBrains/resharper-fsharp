module Module

let getOneInstance () = 1
let oneInstance = 1

do
    for i = 1 to 2 do
        for oneInstance = 1 to 2 do
            let _ = id {selstart}(getOneInstance ()){selend}
            oneInstance
