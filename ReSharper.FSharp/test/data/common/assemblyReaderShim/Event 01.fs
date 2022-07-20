module Module

Class.add_VoidEvent(Class.VoidDelegate(ignore))
Class.VoidEvent.Add(fun (_: int) -> ())

Class.IntEvent.Add(fun (_: int) -> ())
Class.add_IntEvent(Class.IntHandler(fun (_: obj) (_: int) -> ()))

Class.Event.Add(fun (_: System.EventArgs) -> ())
