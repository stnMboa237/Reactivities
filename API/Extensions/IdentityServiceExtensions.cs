using System.Text;
using API.Services;
using Domain;
using Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Persistence;

namespace API.Extensions
{
    public static class IdentityServiceExtensions
    {
        public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration config)
        {

            /*here we are setting up the complexeness of a password from opt.Password.<option name>*/
            services.AddIdentityCore<AppUser>(opt =>
                {
                    opt.Password.RequireDigit = true;
                    opt.Password.RequireNonAlphanumeric = false;
                    opt.User.RequireUniqueEmail = true;
                    opt.SignIn.RequireConfirmedEmail = true;
                }
            ).AddEntityFrameworkStores<DataContext>()
            .AddSignInManager<SignInManager<AppUser>>()
            .AddDefaultTokenProviders();

            /*Set the JWT Token*/
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["TokenKey"]));
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true, /* validate if token is valid token!!! IMPORTANT!!! */
                    IssuerSigningKey = key,  /* the key here must be the same as into TokenServices.cs */
                    ValidateIssuer = false,  /* nous validons pas car nous voulons laisser la validation aussi basique que possible*/
                    ValidateAudience = false,/* nous validons pas car nous voulons laisser la validation aussi basique que possible*/
                    ValidateLifetime = true, /* la durée de vie minimale par defaut d'un token est de 5min. Mais nous avons defini 1min dans la CreateToken á des fins de tests*/
                    ClockSkew = TimeSpan.Zero, /* ceci ramène á zero la durée de vie minimale d'un token cad qu'apres 1 min, le token generé par CreateToken sera NON VALIDE*/
                };
                /*begin: get the from the Header for SignalR*/
                opt.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/chat")))
                        {
                            context.Token = accessToken;
                        };
                        return Task.CompletedTask;
                    }
                };
                /*End: get the from the Header for SignalR*/
            });

            services.AddAuthorization(opt =>
            {
                opt.AddPolicy("IsActivityHost", policy =>
                {
                    policy.Requirements.Add(new IsHostRequirement());
                });
            });
            services.AddTransient<IAuthorizationHandler, IsHostRequirementHandler>();

            services.AddScoped<TokenService>();

            return services;
        }
    }
}