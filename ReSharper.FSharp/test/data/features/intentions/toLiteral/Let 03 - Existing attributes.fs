module Module

let [<CompiledName "I">] i{caret} = 1

// This case ensures "D" expession is successfully 
// converted lazily from FCS AST after the modification.
let [<CompiledName "D">] d = 1.0
