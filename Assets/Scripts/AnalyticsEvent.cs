public class player_death : Unity.Services.Analytics.Event
{
	public player_death() : base("player_death")
	{
	}

    public string level { set { SetParameter("level", value); } }
	public bool level_beaten { set { SetParameter("level_beaten", value); } }
	public float x_pos { set { SetParameter("x_pos", value); } }
	public float y_pos { set { SetParameter("y_pos", value); } }
	public float portal1_x { set {SetParameter("portal1_x", value); } }
	public float portal1_y { set {SetParameter("portal1_y", value); } }
	public float portal2_x { set {SetParameter("portal2_x", value); } }
	public float portal2_y { set {SetParameter("portal2_y", value); } }
	public float timer { set { SetParameter("timer", value); } }
	public float unreset_timer { set { SetParameter("unreset_timer", value); } }
	// Settings
	public int movement_type { set { SetParameter("movement_type", value); } }
}

public class player_reset : Unity.Services.Analytics.Event
{
	public player_reset() : base("player_reset")
	{
	}

    public string level { set { SetParameter("level", value); } }
	public bool level_beaten { set { SetParameter("level_beaten", value); } }
	public float x_pos { set { SetParameter("x_pos", value); } }
	public float y_pos { set { SetParameter("y_pos", value); } }
	public float portal1_x { set {SetParameter("portal1_x", value); } }
	public float portal1_y { set {SetParameter("portal1_y", value); } }
	public float portal2_x { set {SetParameter("portal2_x", value); } }
	public float portal2_y { set {SetParameter("portal2_y", value); } }
	public float timer { set { SetParameter("timer", value); } }
	public float unreset_timer { set { SetParameter("unreset_timer", value); } }
	// Settings
	public int movement_type { set { SetParameter("movement_type", value); } }
}

public class level_start : Unity.Services.Analytics.Event
{
	public level_start() : base("level_start")
	{
	}

	public string level { set { SetParameter("level", value); } }
	public bool level_beaten { set { SetParameter("level_beaten", value); } }
	public int session_time { set { SetParameter("session_time", value); } }
	// Settings
	public int movement_type { set { SetParameter("movement_type", value); } }
}

public class level_complete : Unity.Services.Analytics.Event
{
	public level_complete() : base("level_complete")
	{
	}

	public string level { set { SetParameter("level", value); } }
	/// <summary>
	/// Whether the level had been beaten before this
	/// </summary>
	public bool level_beaten { set { SetParameter("level_beaten", value); } }
	public float portal1_x { set {SetParameter("portal1_x", value); } }
	public float portal1_y { set {SetParameter("portal1_y", value); } }
	public float portal2_x { set {SetParameter("portal2_x", value); } }
	public float portal2_y { set {SetParameter("portal2_y", value); } }
	public int num_deaths { set { SetParameter("num_deaths", value); } }
	public int num_resets { set { SetParameter("num_resets", value); } }
    public float timer { set { SetParameter("timer", value); } }
	public float unreset_timer { set { SetParameter("unreset_timer", value); } }
	// Settings
	public int movement_type { set { SetParameter("movement_type", value); } }
}

public class level_quit : Unity.Services.Analytics.Event
{
	public level_quit() : base("level_quit")
	{
	}

	public string level { set { SetParameter("level", value); } }
	public bool level_beaten { set { SetParameter("level_beaten", value); } }
	public float x_pos { set { SetParameter("x_pos", value); } }
	public float y_pos { set { SetParameter("y_pos", value); } }
	public float portal1_x { set {SetParameter("portal1_x", value); } }
	public float portal1_y { set {SetParameter("portal1_y", value); } }
	public float portal2_x { set {SetParameter("portal2_x", value); } }
	public float portal2_y { set {SetParameter("portal2_y", value); } }
	public int num_deaths { set { SetParameter("num_deaths", value); } }
	public int num_resets { set { SetParameter("num_resets", value); } }
	public float unreset_timer { set { SetParameter("unreset_timer", value); } }
	public int session_time { set { SetParameter("session_time", value); } }
	// Settings
	public int movement_type { set { SetParameter("movement_type", value); } }
}

// When a player is inactive for 5 minutes
public class inactive : Unity.Services.Analytics.Event
{
	public inactive() : base("inactive")
	{
	}

	public string level { set { SetParameter("level", value); } }
	public bool level_beaten { set { SetParameter("level_beaten", value); } }
	public float x_pos { set { SetParameter("x_pos", value); } }
	public float y_pos { set { SetParameter("y_pos", value); } }
	public float portal1_x { set {SetParameter("portal1_x", value); } }
	public float portal1_y { set {SetParameter("portal1_y", value); } }
	public float portal2_x { set {SetParameter("portal2_x", value); } }
	public float portal2_y { set {SetParameter("portal2_y", value); } }
	public int num_deaths { set { SetParameter("num_deaths", value); } }
	public int num_resets { set { SetParameter("num_resets", value); } }
	public float unreset_timer { set { SetParameter("unreset_timer", value); } }
	public int session_time { set { SetParameter("session_time", value); } }
	// Settings
	public int movement_type { set { SetParameter("movement_type", value); } }
}