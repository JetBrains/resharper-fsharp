module Module

match () with
| :? (int -> int) -> ()

match (), 1 with
| :? (int -> int), 1 -> ()
| (:? (int -> int)), 1 -> ()
