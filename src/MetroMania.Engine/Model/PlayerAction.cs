namespace MetroMania.Engine.Model;

public abstract record PlayerAction
{
    public static PlayerAction None => new NoAction();
}

public sealed record NoAction : PlayerAction;
