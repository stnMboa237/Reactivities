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

/* start: config the http Header for security policies(directly after ExecptionMiddleware)*/

app.UseXContentTypeOptions(); //header = X-Content-Type-Options: prevents app from Mime Sniffing pof the content type
app.UseReferrerPolicy(opt => opt.NoReferrer()); //header = Referrer-Policy
app.UseXXssProtection(opt => opt.EnabledWithBlockMode()); // header = Content-Security-Policy: add a protection against the cross site scripting header
app.UseXfo(opt => opt.Deny()); //X-Frame-Options

// we use this in dev Mode to check the headers which we want to enable via the "CustomSource(...)"
// app.UseCspReportOnly(opt => opt //header= Content-Security-Policy-Report-Only
//     .BlockAllMixedContent() //ceci force l'app á charger que du contenu HTTPS ou juste HTTP. Mais pas un mix des 2
//     .StyleSources(s => s.Self()
//         .CustomSources("https://fonts.googleapis.com") // exception: ok pour ce contenu
//     ) //approuve seulement les CSS provenant de notre APP (wwwroot)
//     .FontSources(s => s.Self()
//         .CustomSources("https://fonts.gstatic.com", "data:") // exception: ok pour ce contenu
//     ) //approuve seulement les fonts provenant de notre APP (wwwroot)
//     .FormActions(s => s.Self()) //approuve seulement les form action provenant de notre APP (wwwroot)
//     .FrameAncestors(s => s.Self()) //approuve seulement les frame provenant de notre APP (wwwroot)
//     .ImageSources(s => s.Self().CustomSources("blob:", "https://res.cloudinary.com")) //approuve seulement les images provenant de notre APP (wwwroot)
//     .ScriptSources(s => s.Self()) //approuve seulement les scripts action provenant de notre APP (wwwroot)
// );

app.UseCsp(opt => opt //header= Content-Security-Policy-Report-Only
    .BlockAllMixedContent() //ceci force l'app á charger que du contenu HTTPS ou juste HTTP. Mais pas un mix des 2
    .StyleSources(s => s.Self()
        .CustomSources("https://fonts.googleapis.com") // exception: ok pour ce contenu
    ) //approuve seulement les CSS provenant de notre APP (wwwroot)
    .FontSources(s => s.Self()
        .CustomSources("https://fonts.gstatic.com", "data:") // exception: ok pour ce contenu
    ) //approuve seulement les fonts provenant de notre APP (wwwroot)
    .FormActions(s => s.Self()) //approuve seulement les form action provenant de notre APP (wwwroot)
    .FrameAncestors(s => s.Self()) //approuve seulement les frame provenant de notre APP (wwwroot)
    .ImageSources(s => s.Self().CustomSources("blob:", "https://res.cloudinary.com")) //approuve seulement les images provenant de notre APP (wwwroot)
    .ScriptSources(s => s.Self()) //approuve seulement les scripts action provenant de notre APP (wwwroot)
);

/* end: config the http Header for security policies(directly after ExecptionMiddleware)*/

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}else {
    app.Use(async (context, next) => {
        context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000");
        await next.Invoke();
    });
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
