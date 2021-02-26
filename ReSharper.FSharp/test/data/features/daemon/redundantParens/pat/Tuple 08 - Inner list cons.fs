module Module

let (_, _ :: _) = []

for (_, _ :: _) in [] do ()

match [] with
| (_, _ :: _) -> ()

function (_, _ :: _) -> ()

try () with (_, _ :: _) -> ()
