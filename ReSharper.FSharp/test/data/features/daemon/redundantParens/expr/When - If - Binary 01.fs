module M

match () with
| _ when true = (if () then ()) -> ()
| _ when (if () then ()) = true -> ()
| _ when true = (if () then ()) = true -> ()

| _ when (true = (if () then ())) -> ()
| _ when ((if () then ()) = true) -> ()
| _ when (true = (if () then ()) = true) -> ()
