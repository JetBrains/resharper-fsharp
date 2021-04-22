public interface IBaseInterface
{
  void M1();
  int P1 { get; }
}


public interface IInterface : IBaseInterface
{
  void M2(int i);
  int P2 { get; }
}


public interface IEmptyBaseInterface
{
}

public interface IEmptyInterface : IEmptyBaseInterface
{
}
