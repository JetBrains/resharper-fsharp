module Module

type T() =
    inherit Class()

    let t = T()

    let _: Class = T.StaticField
    let _: Class = T.Field

    let _: Class = Class.StaticField
    let _: Class = Class.Field

    let _: Class = t.Field
    let _: Class = t.StaticField


    let _: string = Class.StaticFieldString


    let _: bool = Class.ProtectedStaticFieldBool
    let _: int16 = t.ProtectedFieldShort

    let _: int16 = Class.ProtectedFieldShort
    let _: bool = t.ProtectedStaticFieldBool


    let _: int = Class.InternalStaticFieldInt
    let _: float = t.InternalFieldDouble

    let _: float = Class.InternalFieldDouble
    let _: int = t.InternalStaticFieldInt


    let _: int = Class.PrivateStaticFieldInt
    let _: float = t.PrivateFieldDouble

    let _: float = Class.PrivateFieldDouble
    let _: int = t.PrivateStaticFieldInt
