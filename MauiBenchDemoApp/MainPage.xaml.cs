namespace MauiBenchDemoApp;

using static PlanetsDrawable;

public partial class MainPage : ContentPage
{
    double Speed { get; set; } = NormalSpeed;

    readonly IDispatcherTimer timer;

    public MainPage()
	{
		InitializeComponent();

        timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(1) / FramesPerSecond;
        timer.Tick += Timer_Tick;

        timer.Start();
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        if (Resources["drawable"] is PlanetsDrawable pd)
        {
            pd.Advance(TimeSpan.FromDays(Speed));

            PlanetsImg.Invalidate();
        }
    }

    private void RewindBtn_Clicked(object sender, EventArgs e)
    {
        if (timer.IsRunning)
        {
            if (Speed > 0)
            {
                Speed /= 2;
            }
            else
            {
                Speed *= 2;
            }

            if (Speed >= 0 && Speed < NormalSpeed)
            {
                PlayBtn_Clicked(sender, e);
            }
        }
        else
        {
            Speed = -NormalSpeed;
            PlayBtn_Clicked(sender, e);
        }
    }

    private void FfBtn_Clicked(object sender, EventArgs e)
    {
        if (timer.IsRunning)
        {
            if (Speed < 0)
            {
                Speed /= 2;
            }
            else
            {
                Speed *= 2;
            }

            if (Speed <= 0 && Speed > -NormalSpeed)
            {
                PlayBtn_Clicked(sender, e);
            }
        }
        else
        {
            Speed = NormalSpeed;
            PlayBtn_Clicked(sender, e);
        }
    }

    private void PlayBtn_Clicked(object sender, EventArgs e)
    {
        if (timer.IsRunning)
        {
            PlayBtn.Text = "▶";
            timer.Stop();
        }
        else
        {
            PlayBtn.Text = "⏸";
            timer.Start();
        }
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        if (Resources["drawable"] is PlanetsDrawable pd)
        {
            pd.OnlyInner = !pd.OnlyInner;
        }
    }
}
