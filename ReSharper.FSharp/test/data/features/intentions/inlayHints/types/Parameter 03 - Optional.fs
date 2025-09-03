// ${BULB_TEXT:Add type annotation}

module Module

type A =
    member _.M1(a, ?x{caret}) = x.Value + 1
