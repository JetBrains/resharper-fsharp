using static Module;

public class Class<T> where T : struct
{
  public Class()
  {
    SU u = SU.NewCase(_item: 123);
    SU uError = SU.NewCase(item: 123);

    int t = u.Tag;
    int i = u.Item;

    bool isCaseError = u.IsCase;
    int tagsError = U.Tags.CaseA;
    U.Case c = (U.Case) u;
  }
}

public class ClassU : Class<U>
{
}

public class ClassSU : Class<SU>
{
}
