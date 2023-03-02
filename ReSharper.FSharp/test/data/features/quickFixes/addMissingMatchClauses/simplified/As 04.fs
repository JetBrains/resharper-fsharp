module Say

match true, true{caret} with
| true, ((_ as true) | false) -> ()
