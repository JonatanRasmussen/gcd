

namespace GlobalNameSpace;

public class Level
{
    private List<Creature> Creatures { get; set; }

    public Level()
    {
        Creatures = new();
    }

    public Level CreateEmpty()
    {
        return new Level();
    }
}