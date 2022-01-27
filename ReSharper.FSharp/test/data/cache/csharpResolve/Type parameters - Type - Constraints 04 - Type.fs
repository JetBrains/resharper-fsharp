namespace global

open System

type BaseClassConstraint1<'T when 'T :> obj>() =
    class end

type BaseClassConstraint2<'T when 'T :> Exception>() =
    class end

type InterfaceConstraint<'T when 'T :> IDisposable>() =
    class end

type UnresolvedTypeConstraint<'T when 'T :> foo>() =
    class end
