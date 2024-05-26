using MonkeyFinder.Services;

namespace MonkeyFinder.ViewModel;

public partial class MonkeysViewModel : BaseViewModel
{
    MonkeyService monkeyService;
    public ObservableCollection<Monkey> Monkeys { get; } = new();


    public Command GetMonkeysCommand { get; }

    IConnectivity connectivity;
    IGeolocation geolocation;
    public MonkeysViewModel(MonkeyService monkeyService, IConnectivity connectivity, IGeolocation geolocation)
    {
        Title = "MonkeyFinder";
        this.monkeyService = monkeyService;

        GetMonkeysCommand = new Command(async () => await GetMonkeysAsync());
        this.connectivity = connectivity;
        this.geolocation = geolocation;
    }

    [ObservableProperty]
    bool isRefreshing;

    [RelayCommand]
    async Task GetClosestMonkeyAsync()
    {
        if (IsBusy || Monkeys.Count == 0)
        {
            return;
        }

        try
        {
            var location = await geolocation.GetLastKnownLocationAsync();
            if (location is null)
            {
                location = await geolocation.GetLocationAsync(
                    new GeolocationRequest
                    {
                        DesiredAccuracy = GeolocationAccuracy.Medium,
                        Timeout = TimeSpan.FromSeconds(30)
                    });
            }
            if(location is null) 
            { 
                return;
            }

            var first = Monkeys.OrderBy(m => 
                location.CalculateDistance(new Location (m.Latitude, m.Longitude) , DistanceUnits.Kilometers) ).FirstOrDefault();

            if (first is null)
            {
                return;
            }

            await Shell.Current.DisplayAlert("Closest Monkey", 
                $"{first.Name}" + " " + $"{first.Location}", "OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await Shell.Current.DisplayAlert("Error!", $"Unable to get closest monkeys!: {ex.Message}", "OK");
        }
    }
    [RelayCommand]
    async Task GoToDetailsAsync(Monkey monkey)
    {

        if (monkey == null)
        {
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(DetailsPage)}", true,
            new Dictionary<string,object>
            {
                {"Monkey", monkey }

            });

    }


    async Task GetMonkeysAsync()
    {
        if (IsBusy)
            return;
        try 
        {
            if(connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                
                await Shell.Current.DisplayAlert("Internet issue",$"Check your internet and try again! ", "OK");

                return;
            }

            IsBusy = true;
            var monkeys = await monkeyService.GetMonkeys();

            if(Monkeys.Count != 0)
            {
                Monkeys.Clear();

            }
            foreach(var monkey in monkeys) 
            {
                Monkeys.Add(monkey);    
            }
        }
        catch(Exception ex) 
        {
            Debug.WriteLine($"Unable to get monkeys: {ex.Message}");
            await Shell.Current.DisplayAlert("Error!", ex.Message, "OK");
        }
        finally 
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }
}
