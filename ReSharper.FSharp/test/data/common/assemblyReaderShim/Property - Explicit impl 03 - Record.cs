public interface I
{
    string P1 { get; }
    string P2 { get; }
}

public record C(int P1) : I
{
    string I.P1 => "";
    string I.P2 => "";
}
