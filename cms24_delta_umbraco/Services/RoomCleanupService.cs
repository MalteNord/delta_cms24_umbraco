using cms24_delta_umbraco.Contexts;

public class RoomCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public RoomCleanupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            Console.WriteLine($"[INFO] Current time: {now:HH:mm}");

            if (now.Hour == 23 && now.Minute == 59)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var roomsToRemove = dbContext.Rooms.Where(r => r.IsEnded).ToList();
                    Console.WriteLine($"[INFO] Found {roomsToRemove.Count} rooms to remove.");

                    if (roomsToRemove.Any())
                    {
                        dbContext.Rooms.RemoveRange(roomsToRemove);
                        await dbContext.SaveChangesAsync(stoppingToken);
                        Console.WriteLine("[INFO] Cleanup completed. Rooms with IsEnded=true were deleted.");
                    }
                    else
                    {
                        Console.WriteLine("[INFO] No rooms with IsEnded=true found.");
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
