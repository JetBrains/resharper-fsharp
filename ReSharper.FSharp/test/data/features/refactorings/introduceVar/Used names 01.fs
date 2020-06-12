module Module

let getOneInstance () = 1
let oneInstance = 1

do
    let _ = id {selstart}(getOneInstance ()){selend}
    oneInstance
