// Program.cs
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);
builder.AddExam();

var app = builder.Build();

app.UseExamApiServicesAsync();

app.Run();