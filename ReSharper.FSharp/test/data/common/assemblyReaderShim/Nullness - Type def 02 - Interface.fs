module Module

let a: IBase<string> = WithoutNull()
let b: IBase<string> = WithNull()
let c: IBase<string | null> = WithNull()
