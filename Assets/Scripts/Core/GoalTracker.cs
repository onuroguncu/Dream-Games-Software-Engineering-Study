using UnityEngine;
using System.Collections.Generic;

public enum ObstacleType { None, Vase, Stone, Chalice }

//tracks how many of each obstacle type exist in the level and how many have been cleared
//obstacles register themselves on init, they call ObstacleCleared when destroyed
//levelmanager polls AllGoalsComplete() after each move to check for a win
public class GoalTracker : MonoBehaviour
{
    public static GoalTracker Instance { get; private set; }

    private Dictionary<ObstacleType, int> totalCounts = new Dictionary<ObstacleType, int>();
    private Dictionary<ObstacleType, int> clearedCounts = new Dictionary<ObstacleType, int>();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    //registers one instance of an obstacle type (used by vase, stone)
    public void RegisterObstacle(ObstacleType type)
    {
        if (!totalCounts.ContainsKey(type)) totalCounts[type] = 0;
        if (!clearedCounts.ContainsKey(type)) clearedCounts[type] = 0;
        totalCounts[type]++;
    }

    //registers multiple chalice units at once (chalicebox has 10 chalices per box)
    public void RegisterObstacleWithCount(int count)
    {
        if (!totalCounts.ContainsKey(ObstacleType.Chalice)) totalCounts[ObstacleType.Chalice] = 0;
        if (!clearedCounts.ContainsKey(ObstacleType.Chalice)) clearedCounts[ObstacleType.Chalice] = 0;
        totalCounts[ObstacleType.Chalice] += count;
    }

    //called when an obstacle is destroyed, updates the hud goal slot
    public void ObstacleCleared(ObstacleType type)
    {
        if (!clearedCounts.ContainsKey(type)) clearedCounts[type] = 0;
        clearedCounts[type]++;
        LevelSceneUI.Instance?.UpdateGoal(type, clearedCounts[type], totalCounts[type]);
    }

    public Dictionary<ObstacleType, int> GetAllTotals() => totalCounts;

    //returns true only when every registered obstacle type is fully cleared
    public bool AllGoalsComplete()
    {
        if (totalCounts.Count == 0) return false;
        foreach (var kvp in totalCounts)
        {
            int cleared = clearedCounts.ContainsKey(kvp.Key) ? clearedCounts[kvp.Key] : 0;
            if (cleared < kvp.Value) return false;
        }
        return true;
    }

    //called at the start of each level to wipe counts from the previous level
    public void Reset()
    {
        totalCounts.Clear();
        clearedCounts.Clear();
    }
}
