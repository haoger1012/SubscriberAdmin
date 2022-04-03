using Microsoft.EntityFrameworkCore;
using SubscriberAdmin;
using WebApplication1.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<SubscriberContext>(options => options.UseInMemoryDatabase("subs"));

builder.Services.AddHttpClient();
builder.Services.AddScoped<LineService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/subs", async (SubscriberContext db) => await db.Subscribers.ToListAsync())
    .WithName("GetSubscribers");

app.MapGet("/subs/{id}", async (SubscriberContext db, int id) =>
{
    var sub = await db.Subscribers.FindAsync(id);
    if (sub is null) return Results.NotFound();
    return Results.Ok(sub);
}).WithName("GetSubscriberById");

app.MapPost("/subs", async (SubscriberContext db, Subscriber sub) =>
{
    await db.Subscribers.AddAsync(sub);
    await db.SaveChangesAsync();
    return Results.Created($"/subs/{sub.Id}", sub);
}).WithName("AddSubscriber");

app.MapPut("/subs/{id}", async (SubscriberContext db, Subscriber updatesub, int id) =>
{
    var sub = await db.Subscribers.FindAsync(id);
    if (sub is null) return Results.NotFound();
    sub.Username = updatesub.Username;
    sub.AccessToken = updatesub.AccessToken;
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/subs/{id}", async (SubscriberContext db, int id) =>
{
    var sub = await db.Subscribers.FindAsync(id);
    if (sub is null)
    {
        return Results.NotFound();
    }
    db.Subscribers.Remove(sub);
    await db.SaveChangesAsync();
    return Results.Ok();
}).WithName("DeleteSubscriber");

app.MapGet("/api/line-login", (LineService service) =>
{
    var url = service.GetLoginUrl();
    return Results.Redirect(url);
});

app.MapGet("/api/line-login-callback", async (LineService service, SubscriberContext db, string state, string code, string error, string error_description) =>
{
    var response = await service.GetLoginResponseAsync(code);

    if (!string.IsNullOrEmpty(response?.IdToken))
    {
        await db.Subscribers.AddAsync(new Subscriber
        {
            Username = response.IdToken
        });
        await db.SaveChangesAsync();
    }

    return Results.Redirect($"/subscriber?idToken={response.IdToken}");
});

app.MapGet("/api/line-notify", (LineService service, string idToken) =>
{
    var url = service.GetNotifyUrl(idToken);
    return Results.Redirect(url);
});

app.MapGet("/api/line-notify-callback", async (LineService service, SubscriberContext db, string state, string code, string error, string error_description) =>
{
    var response = await service.GetNotifyResponseAsync(code);
    var sub = await db.Subscribers.FirstOrDefaultAsync(x => x.Username == state);
    if (sub != null && !string.IsNullOrEmpty(response?.AccessToken))
    {
        sub.AccessToken = response.AccessToken;
        await db.SaveChangesAsync();
    }
    return Results.Redirect($"/subscriber?accessToken={response.AccessToken}");
});

app.MapPost("/api/line-notify-message", async (LineService service, SubscriberContext db, string message) =>
{
    var subs = await db.Subscribers.Where(x => !string.IsNullOrEmpty(x.AccessToken)).ToListAsync();
    var tasks = new List<Task>();
    foreach (var sub in subs)
    {
        tasks.Add(service.SendNotification(sub.AccessToken, message));
    }
    await Task.WhenAll(tasks);
    return Results.Ok();
});

app.MapPost("/api/line-notify-revoke", async (LineService service, SubscriberContext db, string idToken) =>
{
    var sub = await db.Subscribers.FirstOrDefaultAsync(x => x.Username == idToken);
    if (sub != null)
    {        
        await service.RevokeNotification(sub.AccessToken);
        sub.AccessToken = null;
        await db.SaveChangesAsync();
    }
    return Results.Ok();
});

app.MapFallbackToFile("/index.html");

app.Run();
