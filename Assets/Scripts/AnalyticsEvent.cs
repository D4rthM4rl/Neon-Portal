public class player_death : Unity.Services.Analytics.Event
{
	public player_death() : base("player_death")
	{
	}

    public string level { set { SetParameter("level", value); } }
	public float x_pos { set { SetParameter("x_pos", value); } }
	public float y_pos { set { SetParameter("y_pos", value); } }
	public float timer { set { SetParameter("timer", value); } }
}

public class player_reset : Unity.Services.Analytics.Event
{
	public player_reset() : base("player_reset")
	{
	}

    public string level { set { SetParameter("level", value); } }
	public float x_pos { set { SetParameter("x_pos", value); } }
	public float y_pos { set { SetParameter("y_pos", value); } }
	public float timer { set { SetParameter("timer", value); } }
}

public class level_complete : Unity.Services.Analytics.Event
{
	public level_complete() : base("level_complete")
	{
	}

	public string level { set { SetParameter("level", value); } }
	public int num_deaths { set { SetParameter("num_deaths", value); } }
	public int num_resets { set { SetParameter("num_resets", value); } }
    public float timer { set { SetParameter("timer", value); } }
}



