module M

match () with
| _ when (true && true) -> ()
| _ when true && (true && true) -> ()
| _ when (true && true) && true -> ()
