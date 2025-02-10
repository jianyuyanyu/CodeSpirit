public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, ILogger<SeederService> logger)
    {
        SeederService seederService = serviceProvider.GetRequiredService<SeederService>();
        await seederService.SeedAsync();
    }
}



