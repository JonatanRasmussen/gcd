

namespace GlobalNameSpace;

public class GameLoop
{
    private int CurrentTurn { get; set; }
    private Level Level { get; set; }
    public bool LevelWon { get; set; }
    public bool LevelLost { get; set; }

    public GameLoop(Level level)
    {
        Level = level;
        CurrentTurn = 0;
        LevelWon = false;
        LevelLost = false;
    }

    public void EnterGameLoop()
    {
        while (LevelWon == false || LevelLost == false)
        {
            TakeTurn();
        }
    }

    private void TakeTurn()
    {
        PromptPlayerAction();
        CurrentTurn++;
    }

    private void PromptPlayerAction()
    {

    }
}