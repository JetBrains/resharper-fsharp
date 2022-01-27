namespace global

type ReferenceConstraint<'T when 'T: not struct>() =
    class end

type NullConstraint<'T when 'T: null>() =
    class end
