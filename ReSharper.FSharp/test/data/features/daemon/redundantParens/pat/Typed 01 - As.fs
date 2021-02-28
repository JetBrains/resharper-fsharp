module Module

let f ((_: int) as a) = ()

for (a: int) as b in [] do ()

match 1, 2 with
| (a: int) as b -> ()

function (a: int) as b -> ()

try () with (a: int) as b -> ()
