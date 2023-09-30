

namespace GlobalNameSpace;

public class LevelPicker
{
    private int CurrentLevelIndex { get; set; }
    private int InitialLevelIndex { get; set; }
    private List<Level> Levels { get; set; }

    public LevelPicker()
    {
        InitialLevelIndex = 0;
        CurrentLevelIndex = InitialLevelIndex;
        Levels = InitializeLevels();
    }

    public Level LoadLevel()
    {
        return Levels[CurrentLevelIndex];
    }

    public void ProceedToNextLevel()
    {
        CurrentLevelIndex++;
    }

    public void ResetToInitialLevel()
    {
        CurrentLevelIndex = InitialLevelIndex;
    }

    public static Level CreateEmpty()
    {
        return new Level();
    }

    private static List<Level> InitializeLevels()
    {
        return new();
    }
}