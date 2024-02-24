/* using System;
using System.Collections.Generic;

namespace GlobalNameSpace;

public enum LevelResult
{
    LevelWon,
    LevelLost,
    LevelHasNotYetEnded,
    GameQuit,
}

public class NewGame
{
    private int CurrentLevel { get; set; }
    private int PlayerScore { get; set; }
    private bool GameHasBeenQuit { get; set; }

    public NewGame()
    {
        CurrentLevel = 0;
        PlayerScore = 0;
        GameHasBeenQuit = false;
    }

    public void StartNewGame()
    {
        while (GameHasBeenQuit == false)
        {
            var selectedLevel = new LevelSelector(CurrentLevel).AutoSelectLevel();
            var levelResult = new GameLoop(selectedLevel).EnterGameLoop();
            ProcessLevelResult(levelResult);
        }
    }

    private void ProcessLevelResult(LevelResult result)
    {
        switch (result)
        {
            case LevelResult.LevelWon:
                ProcessLevelWon();
                break;

            case LevelResult.LevelLost:
                ProcessLevelLost();
                break;

            case LevelResult.GameQuit:
                ProcessGameQuit();
                break;

            default:
                var unhandeledResult = Enum.GetName(typeof(LevelResult), result);
                Console.WriteLine($"Error! Unexpected result: {unhandeledResult}");
                ProcessGameQuit();
                break;
        }
    }

    private void ProcessLevelWon()
    {
        Console.WriteLine($"Victory! Total Score: {PlayerScore}");
        Console.WriteLine($"Proceeding to Next Level...");
        CurrentLevel++;
    }

    private void ProcessLevelLost()
    {
        Console.WriteLine($"Defeat! Final score: {PlayerScore}");
        Console.WriteLine("Your progress has been saved. Returning to Level Selection...");
        PlayerScore = 0;
        CurrentLevel = 0;
    }

    private void ProcessGameQuit()
    {
        Console.WriteLine($"Game has been abandoned. Final score: {PlayerScore}");
        Console.WriteLine("The program will now terminate. Thank you for playing!");
        GameHasBeenQuit = true;
    }
}

public class LevelSelector
{
    private int CurrentLevelIndex { get; set; }

    public LevelSelector(int currentLevelIndex)
    {
        CurrentLevelIndex = currentLevelIndex;
    }

    public Level AutoSelectLevel()
    {
        return LevelCollection.GetLevel(CurrentLevelIndex);
    }
}

public static class LevelCollection
{
    public static Level GetLevel(int levelIndex)
    {
        var levelBuilder = new LevelBuilder();
        var levelTemplate = LevelTable[levelIndex];
        levelBuilder.LoadLevelTemplate(levelTemplate);
        return levelBuilder.Build();
    }

    public static Dictionary<int, ILevelTemplate> LevelTable
    {
        get
        {
            var levelOrder = new Dictionary<int, ILevelTemplate>
            {
                { 0, new EmptyLevelTemplate() },
                { 1, new Level01Template() },
            };
            return levelOrder;
        }
    }
}

public interface ILevelTemplate
{
    List<ScriptedObject> LevelContent { get; }
}

public class EmptyLevelTemplate : ILevelTemplate
{
    public List<ScriptedObject> LevelContent
    {
        get
        {
            return new List<ScriptedObject>();
        }
    }
}

public class Level01Template : ILevelTemplate
{
    public List<ScriptedObject> LevelContent
    {
        get
        {
            return new List<ScriptedObject>()
            {
                ScriptedObject.CreateEmpty(),
            };
        }
    }
}

public class LevelBuilder
{
    private List<ScriptedObject> ScriptedObjects { get; set; }

    public LevelBuilder()
    {
        ScriptedObjects = new List<ScriptedObject>();
    }

    public void LoadLevelTemplate(ILevelTemplate levelTemplate)
    {
        var gameObjects = levelTemplate.LevelContent;
        ScriptedObjects.AddRange(gameObjects);
    }

    public void AddScriptedObject(ScriptedObject scriptedObject)
    {
        ScriptedObjects.Add(scriptedObject);
    }

    public Level Build()
    {
        return new Level(ScriptedObjects);
    }
}

public class Level
{
    private List<ScriptedObject> ScriptedObjects { get; }

    public Level(List<ScriptedObject> scriptedObjects)
    {
        ScriptedObjects = scriptedObjects;
    }

    public static Level CreateEmpty()
    {
        var emptyList = new List<ScriptedObject>();
        return new Level(emptyList);
    }

    public List<ScriptedObject> GetScriptedObjects()
    {
        return ScriptedObjects;
    }
}

public class GameLoop
{
    private int GameLoopClock { get; set; }
    private CombatLog CombatLog { get; set; }
    private EffectExecuter EffectExecuter { get; }
    private List<ScriptedObject> ScriptedObjects { get; set; }
    private LevelResult LevelResult { get; set; }

    public GameLoop(Level level)
    {
        GameLoopClock = 0;
        CombatLog = new();
        EffectExecuter = new();
        ScriptedObjects = level.GetScriptedObjects();
        LevelResult = LevelResult.LevelHasNotYetEnded;
    }

    public LevelResult EnterGameLoop()
    {
        while (LevelResult == LevelResult.LevelHasNotYetEnded)
        {
            foreach (var scriptedObject in ScriptedObjects)
            {
                ProcessScriptedObjects(scriptedObject);
            }
            UpdateLevelResult();
            GameLoopClock++;
        }
        return LevelResult;
    }

    private void ProcessScriptedObjects(ScriptedObject scriptedObject)
    {
        var scriptedEvent = scriptedObject.GetScriptedEvent(GameLoopClock);
        scriptedEvent.SetCaster(scriptedObject);
        scriptedEvent.SelectTargets(ScriptedObjects);
        bool successfulCast = EffectExecuter.ProcessScriptedEvent(scriptedEvent);
        if (successfulCast)
        {
            CombatLog.RegisterEvent(scriptedEvent);
        }
    }

    private void UpdateLevelResult()
    {
        LevelResult = LevelResult.LevelHasNotYetEnded;
    }
}

public class EffectExecuter
{
    private ScriptedEvent ScriptedEvent { get; set; }

    public EffectExecuter()
    {
        ScriptedEvent = ScriptedEvent.CreateEmpty();
    }

    public bool ProcessScriptedEvent(ScriptedEvent scriptedEvent)
    {
        ScriptedEvent = scriptedEvent;

        bool successfulCast = AttemptToCast();
        if (successfulCast)
        {
            foreach (var target in ScriptedEvent.GetTargets())
            {
                ApplyEffectOnTarget(target);
            }
        }
        return successfulCast;
    }

    private bool AttemptToCast()
    {
        // Check if Caster is able to cast
        return true;
    }

    private void ApplyEffectOnTarget(ScriptedObject target)
    {
        // Carry out effect
    }
}

public class CombatLog
{
    private List<ScriptedEvent> ScriptedEvents { get; }

    public CombatLog()
    {
        ScriptedEvents = new();
    }

    public void RegisterEvent(ScriptedEvent scriptedEvent)
    {
        ScriptedEvents.Add(scriptedEvent);
    }
}

public class ScriptedObject
{
    private int ID { get; }
    private Dictionary<int, ScriptedEvent> Script { get; }

    public ScriptedObject(int id, Dictionary<int, ScriptedEvent> script)
    {
        ID = id;
        Script = script;
    }

    public ScriptedEvent GetScriptedEvent(int GameLoopClock)
    {
        if (Script.ContainsKey(GameLoopClock))
        {
            return Script[GameLoopClock];
        }
        else
        {
            return ScriptedEvent.CreateEmpty();
        }
    }

    public void PerformScriptedEvent(ScriptedEvent scriptedEvent)
    {
        if (ID == -1)
        {

        }
    }

    public static ScriptedObject CreateEmpty()
    {
        return new ScriptedObject(0, new());
    }
}

public static class ScriptedObjectFilters
{
    public static List<ScriptedObject> FilterAllies(List<ScriptedObject> scriptedObjects)
    {
        return scriptedObjects;
    }
}
public class ScriptedEvent
{
    private ITargetSelector TargetSelector { get; set; }
    private ScriptedObject Caster { get; set; }
    private List<ScriptedObject> Targets { get; set; }

    public ScriptedEvent()
    {
        TargetSelector = new EmptyTargetSelector();
        Caster = ScriptedObject.CreateEmpty();
        Targets = new();
    }

    public void SetCaster(ScriptedObject caster)
    {
        Caster = caster;
    }

    public void SelectTargets(List<ScriptedObject> availableTargets)
    {
        Targets = TargetSelector.FindTarget(availableTargets);
    }

    public List<ScriptedObject> GetTargets()
    {
        return Targets;
    }

    public static ScriptedEvent CreateEmpty()
    {
        return new ScriptedEvent();
    }
}

public interface ITargetSelector
{
    List<ScriptedObject> FindTarget(List<ScriptedObject> availableTargets);
}

public class EmptyTargetSelector : ITargetSelector
{
    public List<ScriptedObject> FindTarget(List<ScriptedObject> availableTargets)
    {
        return new List<ScriptedObject>();
    }
}

public class TargetOneEnemy : ITargetSelector
{
    public List<ScriptedObject> FindTarget(List<ScriptedObject> availableTargets)
    {
        return new List<ScriptedObject>()
        {
            availableTargets[0],
        };
    }
}

public class TargetTwoEnemies : ITargetSelector
{
    public List<ScriptedObject> FindTarget(List<ScriptedObject> availableTargets)
    {
        return new List<ScriptedObject>()
        {
            availableTargets[0],
            availableTargets[1],
        };
    }
} */