public interface IBaseInterface
{
  void M1();
}


public interface IInterface : IBaseInterface
{
  void M2(int i);
}


public interface IEmptyBaseInterface
{
}

public interface IEmptyInterface : IEmptyBaseInterface
{
}
