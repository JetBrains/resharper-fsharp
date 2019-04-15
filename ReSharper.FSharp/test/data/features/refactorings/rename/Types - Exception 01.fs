//${NEW_NAME:Zzz}
module Module

exception Exn of int

let e1 = Exn{caret} 123
let e2 = Unchecked.defaultof<Exn>
