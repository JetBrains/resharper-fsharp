module Module

let boxRef (s: RefStruct) = box s
let boxPlain (s: PlainStruct) = box s

let listRef (s: RefStruct) = [ s ]
let listPlain (s: PlainStruct) = [ s ]
