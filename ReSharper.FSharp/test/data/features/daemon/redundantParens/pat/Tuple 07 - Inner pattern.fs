module Module

let (a, b: int) = 1, 2

for (a, b: int) in [] do ()
for (a: int, b) in [] do ()

match 1, 2 with
| (a, b: int) -> ()

function (a, b: int) -> ()

try () with (a, b: int) -> ()
