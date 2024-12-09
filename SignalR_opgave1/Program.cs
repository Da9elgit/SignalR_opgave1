using SignalR_opgave1;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

//app.MapGet("/", () => "Hello World!");
app.MapHub<GameHub>("/GameHub");
app.Run();
