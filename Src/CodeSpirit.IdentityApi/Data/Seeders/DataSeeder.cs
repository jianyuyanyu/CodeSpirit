using CodeSpirit.IdentityApi.Data.Seeders;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        SeederService seederService = serviceProvider.GetRequiredService<SeederService>();
        await seederService.SeedAsync();
    }
}



