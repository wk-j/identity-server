﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Newtonsoft.Json.Linq;

namespace GitHub {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services) {
            services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = "GitHub";
            })
            .AddCookie()
            // .AddJwtBearer(cfg => {
            //     cfg.RequireHttpsMetadata = false;
            //     cfg.Authority = "https://github.com/login/oauth/authorize";
            //     cfg.IncludeErrorDetails = true;
            //     cfg.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters() {
            //         ValidateAudience = false,
            //         ValidateIssuerSigningKey = true,
            //         ValidateIssuer = true,
            //         ValidIssuer = "https://github.com/login/oauth/authorize",
            //         ValidateLifetime = true,
            //     };
            //     cfg.Events = new JwtBearerEvents() {
            //         OnAuthenticationFailed = c => {
            //             c.NoResult();
            //             c.Response.StatusCode = 401;
            //             c.Response.ContentType = "text/plain";
            //             return c.Response.WriteAsync(c.Exception.ToString());
            //         }
            //     };
            // })
            .AddOAuth("GitHub", options => {
                options.ClientId = "9f18bf43ed3bce16e685";
                options.ClientSecret = "78331982064908b8a16725e44843e23481fbae46";
                options.CallbackPath = new Microsoft.AspNetCore.Http.PathString("/signin");
                options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
                options.TokenEndpoint = "https://github.com/login/oauth/access_token";
                options.UserInformationEndpoint = "https://api.github.com/user";

                options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
                options.ClaimActions.MapJsonKey("urn:github:login", "login");
                options.ClaimActions.MapJsonKey("urn:github:url", "html_url");
                options.ClaimActions.MapJsonKey("urn:github:avatar", "avatar_url");

                options.SaveTokens = true;

                options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents {
                    OnCreatingTicket = async context => {
                        var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);

                        var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseContentRead, context.HttpContext.RequestAborted);
                        response.EnsureSuccessStatusCode();

                        Console.WriteLine("Token = {0}", context.AccessToken);

                        var user = JObject.Parse(await response.Content.ReadAsStringAsync());
                        Console.WriteLine(user.ToString());
                        context.RunClaimActions(user);
                    }
                };
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseHsts();
            }

            IdentityModelEventSource.ShowPII = true;

            // app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseMvc(routes => {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action=Index}/{id?}");
            });
        }
    }
}
