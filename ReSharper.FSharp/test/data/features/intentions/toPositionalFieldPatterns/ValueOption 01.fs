module Module

match ValueNone with
| ValueSome(Item{caret} = 1) -> ()
| _ -> ()
