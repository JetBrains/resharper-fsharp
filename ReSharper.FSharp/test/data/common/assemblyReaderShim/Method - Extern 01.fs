module Module

let ni = nativeint 1

let _: int = Class.ExternMethod1(1)
let _: int = Class.ExternMethod1(ni)

let _: int = Class.ExternMethod2(1, "")
let _: int = Class.ExternMethod2(ni, "")

let _: int = Class.ExternMethod3(1)
