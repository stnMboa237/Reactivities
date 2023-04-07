using API.Extensions;
using API.Middleware;
using API.SignalR;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(opt =>
{
    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    opt.Filters.Add(new AuthorizeFilter(policy));
});

/* adding our customs services before the app builds*/
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
/* end app build*/

var app = builder.Build();

/*il est imperatif d'injecter la dépendance des exception (gestions des erreurs)
dès que l'app compile afin que toutes les eventuelles erreurs générées pas des services
plus bas soient gerées par le middlware
*/
app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//attention á la disposition des middleware: 
//on veut que les Cors Policy soit injectées avant que l'app n'envoit 
//la requete en pre-flight et surtout avant L'AUTHENTIFICATION
app.UseCors("CorsPolicy");

// app.UseHttpsRedirection();

app.UseAuthentication(); /*AUTHENTICATION CAME'S ALWAYS FIRST BEFORE AUTHORIZATION*/
app.UseAuthorization();

app.UseDefaultFiles(); /*tells to Kestrel server to look html files into wwwroot folder and fetch them*/
app.UseStaticFiles();  /*tells to Kestrel server to look static files (*.js, ) into wwwroot folder and fetch them*/

app.MapControllers();
app.MapHub<ChatHub>("/chat"); //tout juste après MapControllers, on doit mapper
// ChatHub et indiquer la route oú seront redirigés les user quand ils se connecteront á notre chatHub

app.MapFallbackToController("Index", "Fallback"); 

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;

try
{
    var context = services.GetRequiredService<DataContext>();
    var userManager = services.GetRequiredService<UserManager<AppUser>>();
    await context.Database.MigrateAsync();
    await Seed.SeedData(context, userManager);
}
catch (Exception ex)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An Error occured during migration");
}
app.Run();
