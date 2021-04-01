module Module

let c: Class = Class.StaticField
let _: Class = Class.Field

let _: Class = c.Field
let _: Class = c.StaticField


let _: string = Class.StaticFieldString


let _: bool = Class.ProtectedStaticFieldBool
let _: int16 = c.ProtectedFieldShort

let _: int16 = Class.ProtectedFieldShort
let _: bool = c.ProtectedStaticFieldBool


let _: int = Class.InternalStaticFieldInt
let _: float = c.InternalFieldDouble

let _: float = Class.InternalFieldDouble
let _: int = c.InternalStaticFieldInt


let _: int = Class.PrivateStaticFieldInt
let _: float = c.PrivateFieldDouble

let _: float = Class.PrivateFieldDouble
let _: int = c.PrivateStaticFieldInt
