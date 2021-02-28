module Module

let (a, b: int) = 1, 2
let (a: int, b) = 1, 2

for (a, b: int) in [] do ()
for (a: int, b) in [] do ()

match 1, 2 with
| (a, b: int) -> ()
| (a, b: int) when true -> ()
| (a: int, b) -> ()

function (a, b: int) -> ()
function (a: int, b) -> ()

try () with (a, b: int) -> ()
try () with (a: int, b) -> ()
