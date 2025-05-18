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
}