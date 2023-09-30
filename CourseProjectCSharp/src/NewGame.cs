

namespace GlobalNameSpace;

public class NewGame
{

    private LevelPicker LevelPicker { get; set; }
    private Level CurrentLevel { get; set; }
    private int PlayerScore { get; set; }
    private bool GameOver { get; set; }
    public NewGame()
    {
        LevelPicker = new();
        CurrentLevel = LevelPicker.CreateEmpty();
        GameOver = false;
    }

    public void PlayGame()
    {
        while (GameOver == false)
        {
            CurrentLevel = LevelPicker.LoadLevel();
            GameLoop gameLoop = new(CurrentLevel);
            gameLoop.EnterGameLoop();
            if (gameLoop.LevelWon == true)
            {
                LevelPicker.ProceedToNextLevel();
                CurrentLevel = LevelPicker.LoadLevel();
            }
            else if (gameLoop.LevelLost == true)
            {
                GameOver = true;
            }
            else
            {
                Console.WriteLine($"Error: Level gameLoop has been exist without being won or lost");
            }
        }
        Console.WriteLine($"Game over. Final score: {PlayerScore}");
    }
}