module M

type C1<'T>() = class end

type C2<[<Measure>] 'T1>() = class end

type C3<'T1, [<Measure>] 'T2>() = class end
