module Module{caret}

open System.Runtime.CompilerServices

// Redundant Extension attribute
[<Extension>]
type Extensions = class end


// Redundant backticks
let ``name`` = 1


// Redundant new
let _ = new obj()


// Redundant parens in expr
((5 + 5))
(((5 + 5)))

// Redundant parens in pattern
let (x) = 1

// Redundant parens in type usage
let _: (int) = 1

// Redundant parens in attribute
// Redundant attribute suffix
[<AutoOpenAttribute()>]
module A = ()


// Cons with empty list pat
match [] with
| x :: [] -> ()

// Redundant 'as' pattern
let _ as y = 1


// Redundant application
1 |> id


// Lambda can be replaced with builtin function
fun x -> x

// Lambda can be replaced with it's body
let f x = x
((fun x -> f x)) |> id

// Dot-lambda can be used
fun x -> x.ToString()


// Convert to string interpolation
printf "Replace lambda with '%i'" 5

// Redundant string interpolation
$""


// Redundant indexer dot
[0].[0]


// Nested record update can be simplified
type Record0 = { Foo: int }
type Record1 = { Zoo: Record0 }
let g item = { item with Zoo = { item.Zoo with Foo = 3 } }
