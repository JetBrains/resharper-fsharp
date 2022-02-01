namespace global

type DefaultCtorConstraint<'T when 'T: (new: unit -> 'T)>() =
    class end

type DefaultCtorConstraint1<'T when 'T: (new: (unit) -> 'T)>() =
    class end

type DefaultCtorConstraint2<'T when 'T: (new: unit -> ('T))>() =
    class end

type DefaultCtorConstraint3<'T when 'T: (new: (unit) -> ('T))>() =
    class end

type DefaultCtorConstraint4<'T when 'T: (new: (unit -> 'T))>() =
    class end
