module Module

let (|TryParseBool|_|) (_: string) = Some(true)

let f{caret} (TryParseBool enabled as x) = ()
