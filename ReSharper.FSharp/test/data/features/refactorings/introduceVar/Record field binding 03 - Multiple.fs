//${OCCURRENCE0:Replace 2 occurrences}

module Module

type R = { Foo: string; Bar: string }

let _ = { Foo = {selstart}id ""{selend}{caret}; Bar = id "" }
