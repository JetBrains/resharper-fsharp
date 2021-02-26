module Module

let (a: int as b) = 1
let ((a: int) as b) = 1

let (a: int as b) as c = 1
let (a: int as b as c) = 1
let (a: int as b as c) as d = 1

function (a: int as b) -> ()

match 1 with
| (a: int as b) -> ()
