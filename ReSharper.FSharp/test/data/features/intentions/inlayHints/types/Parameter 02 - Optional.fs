// ${BULB_TEXT:Add type annotation}

module Module

type A =
    member _.M1(?x{caret}) = x.Value + 1
