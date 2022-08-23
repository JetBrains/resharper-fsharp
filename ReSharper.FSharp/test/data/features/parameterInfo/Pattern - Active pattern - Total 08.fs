let (|A|B|) (s: string) = if true then A 1 else B ""

match "" with
| B _{caret} -> ()
