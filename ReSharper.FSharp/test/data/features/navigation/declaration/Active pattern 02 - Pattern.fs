module Module

let (|A|_|) x = Some ()

let (|Id|) f x = x

match () with
| Id (|A|_{on}|) () -> ()
