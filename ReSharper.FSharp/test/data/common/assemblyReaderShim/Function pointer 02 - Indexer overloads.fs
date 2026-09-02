module Module

let singleIndexer = SingleIndexer()
let pointerAndString = PointerAndString()

let _: int = singleIndexer[0n]
let _: int = pointerAndString[0n]
let _: int = pointerAndString[""]

let argTypes = ArgTypes()
let byrefArg = ByrefArg()
let byrefReturn = ByrefReturn()

let _: int = argTypes[0n]
let _: int = byrefArg[0n]
let _: int = byrefReturn[0n]
