type R = | R of a: int
match R(0) with
| R(a= a{caret}) -> ()
