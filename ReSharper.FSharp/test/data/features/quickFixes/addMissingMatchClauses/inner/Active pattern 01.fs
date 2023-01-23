module Say

let (|A|B|``C D``|) x = if true then A else ``C D``

match true{caret} with
| A -> ()
