module Module

let f (v{caret}: Map<'a, _> when 'a : not struct) =
    ()
