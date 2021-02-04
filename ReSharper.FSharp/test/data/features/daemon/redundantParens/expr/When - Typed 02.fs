module M

match () with
| _ when true && (() :? unit) -> ()
