module Module

let _: DefaultCtor = DefaultCtor()
let _: DefaultCtor = new DefaultCtor()

type T1() =
    inherit DefaultCtor()


let _: ExplicitCtor = ExplicitCtor()
let _: ExplicitCtor = new ExplicitCtor()

type T2() =
    inherit ExplicitCtor()


let _: ExplicitCtorProtected = ExplicitCtorProtected()
let _: ExplicitCtorProtected = new ExplicitCtorProtected()

type T3() =
    inherit ExplicitCtorProtected()


let _: ExplicitCtorPrivate = ExplicitCtorPrivate()
let _: ExplicitCtorPrivate = new ExplicitCtorPrivate()

type T4() =
    inherit ExplicitCtorPrivate()


let _: ExplicitCtorParam = ExplicitCtorParam(1)
let _: ExplicitCtorParam = new ExplicitCtorParam(1)

let _: ExplicitCtorParam = ExplicitCtorParam()
let _: ExplicitCtorParam = new ExplicitCtorParam()


let _: ExplicitCtorOverloads = ExplicitCtorOverloads()
let _: ExplicitCtorOverloads = new ExplicitCtorOverloads()

let _: ExplicitCtorOverloads = ExplicitCtorOverloads("")
let _: ExplicitCtorOverloads = new ExplicitCtorOverloads("")

let _: ExplicitCtorOverloads = ExplicitCtorOverloads(1)
let _: ExplicitCtorOverloads = new ExplicitCtorOverloads(1)

let _: ExplicitCtorOverloads = ExplicitCtorOverloads("", 1)
let _: ExplicitCtorOverloads = new ExplicitCtorOverloads("", 1)
