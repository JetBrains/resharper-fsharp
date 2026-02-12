module Module

type R = { F: int }

match { F = 1 } with
| { F = {caret} }
