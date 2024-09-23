public abstract class BaseClass
{
    public virtual void MPublic() { }
    protected virtual void MProtected() { }
    private virtual void MPrivate() { }

    public void NonVirtual() { }
    public void NonVirtualOverriden() { }
}

public class InheritedClass : BaseClass
{
    public override void MPublic() { }
    protected override void MProtected() { }
    private override void MPrivate() { }

    public override void NonVirtualOverriden() { }
}
