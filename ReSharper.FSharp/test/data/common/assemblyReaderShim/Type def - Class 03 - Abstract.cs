public abstract class Class
{
  public abstract void M1();
  public void M2()
  {
  }
}

public abstract class A
{
  public virtual void M1()
  {
  }

  public virtual void M2()
  {
  }

  public virtual void M3()
  {
  }
  
  public virtual void M4()
  {
  }
}

public class B : A
{
  public override void M1()
  {
  }

  public void M2()
  {
  }

  public new void M3()
  {
  }
  
  public new virtual void M4()
  {
  }
}
