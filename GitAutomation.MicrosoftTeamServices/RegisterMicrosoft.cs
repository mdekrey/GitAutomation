using GitAutomation.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace GitAutomation.MicrosoftTeamServices
{
    public class RegisterMicrosoft : IRegisterAuthentication
    {
        public void RegisterAuthentication(IServiceCollection services, AuthenticationBuilder authBuilder, IConfiguration configuration)
        {
            var authenticationOptions = configuration.Get<Auth.AuthenticationOptions>();
            authBuilder
                .AddMicrosoftAccount(Auth.Constants.AuthenticationScheme, options =>
                {
                    var additionalOptions = new AdditionalOptions();
                    configuration.GetSection("activeDirectory").Bind(options);
                    configuration.GetSection("activeDirectory").Bind(additionalOptions);
                    if (additionalOptions.TenantId != null)
                    {
                        var resource = "https://graph.microsoft.com";
                        options.AuthorizationEndpoint = $"https://login.microsoftonline.com/{additionalOptions.TenantId}/oauth2/authorize?resource={resource}";
                        options.TokenEndpoint = $"https://login.microsoftonline.com/{additionalOptions.TenantId}/oauth2/token?resource={resource}";
                    }
                    options.CallbackPath = new PathString("/custom-oauth-signin");
                    options.ClaimActions.DeleteClaim(ClaimTypes.Name);
                    options.ClaimActions.DeleteClaim(ClaimTypes.Email);
                    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "displayName");
                    options.ClaimActions.MapJsonKey(ClaimTypes.Email, "userPrincipalName");
                    options.Events = new OAuthEvents
                    {
                        OnCreatingTicket = async context =>
                        {
                            // Get the GitHub user
                            var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/");
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                            var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
                            response.EnsureSuccessStatusCode();

                            var contents = await response.Content.ReadAsStringAsync();
                            Console.WriteLine(contents);
                            var user = JObject.Parse(contents);

                            context.RunClaimActions(user);
                        }
                    };
                });
        }

    }
}
