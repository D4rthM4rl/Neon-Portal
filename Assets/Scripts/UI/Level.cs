/// <summary>
/// A game level, identified by world and level numbers. Also tracks best time
/// and whether it's been beaten.
/// </summary>
[System.Serializable]
public class Level
{
    public int world;
    public int level;

    public float bestTime;
    public bool beaten;
    public Level(int world, int level)
    {
        this.world = world;
        this.level = level;
        this.bestTime = 0;
        this.beaten = false;
    }

	public override string ToString()
	{
		return "W" + world + "L" + level;
	}
}