type A = | A of a: (int * int) * b: string

match A((1,2),"") with
| A((_, _){caret}, c) -> ()
