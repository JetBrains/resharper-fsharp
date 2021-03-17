type T1 =
    static member Method<'a>() = ()

module Module =
    type T2() = class end

let _ = T1.Method<Module.T2>
