public class InheritAbstractClass : Module.AbstractClass;

public class InheritAbstractClassOverrides : Module.AbstractClass
{
    public override void M1() => base.M1();
    public override void M2(int i) => base.M2(i);
    public override void M3<P1>(int i) => base.M3<P1>(i);
    public override void M4<P1>(P1 p1) => base.M4(p1);
    public override int P1 { get; }
}

public class InheritGenericAbstract : Module.GenericAbstractClass<int>;

public class InheritGenericAbstractOverrides : Module.GenericAbstractClass<int>
{
    public override void M1() => base.M1();
    public override void M2(int i) => base.M2(i);
    public override void M3<P1>(int i) => base.M3<P1>(i);
    public override void M4<P1>(P1 p1) => base.M4(p1);
    public override void M5<P1>(P1 p1, int i) => base.M5<P1>(p1, i);
    public override int P1 { get; }
}

public class InheritGeneric : Module.GenericClass<int>;

public class InheritGenericOverrides : Module.GenericClass<int>
{
    public override void M() => base.M();
    public override void M2(int i) => base.M2(i);
    public override void M3<P1>(int i) => base.M3<P1>(i);
    public override void M4(int i) => base.M4(i);
    public override void M5<P1>(P1 p1, int i) => base.M5<P1>(p1, i);
}

public sealed class Counter;
public sealed class CounterState;

public class InheritGenericCommand2 : Module.GenericAbstractClass2<Counter, CounterState, bool, int>;

public class InheritGenericCommand2Overrides : Module.GenericAbstractClass2<Counter, CounterState, bool, int>
{
    public override bool M(CounterState state, bool input) => base.M(state, input);
}
