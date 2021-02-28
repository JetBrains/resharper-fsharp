match [] with
| a | (_ as a) -> ()
| a | (_ as a) | a -> ()
| (_ as a) | a -> ()
