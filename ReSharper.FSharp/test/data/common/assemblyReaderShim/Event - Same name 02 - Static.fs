module Module

Class1.add_Event(fun (_: System.EventArgs) -> ())
Class1.add_Event(VoidDelegate(ignore))
Class1.add_StaticEvent(fun (_: System.EventArgs) -> ())
Class1.add_StaticEvent(VoidDelegate(ignore))

Class1().add_Event(fun (_: System.EventArgs) -> ())
Class1().add_Event(VoidDelegate(ignore))
Class1().add_StaticEvent(fun (_: System.EventArgs) -> ())
Class1().add_StaticEvent(VoidDelegate(ignore))

Class2.add_Event(fun (_: System.EventArgs) -> ())
Class2.add_Event(VoidDelegate(ignore))
Class2.add_StaticEvent(fun (_: System.EventArgs) -> ())
Class2.add_StaticEvent(VoidDelegate(ignore))

Class2().add_Event(fun (_: System.EventArgs) -> ())
Class2().add_Event(VoidDelegate(ignore))
Class2().add_StaticEvent(fun (_: System.EventArgs) -> ())
Class2().add_StaticEvent(VoidDelegate(ignore))
