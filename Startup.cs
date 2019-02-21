using Api.Helpers;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Api
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public ApiConfig ApiConfig;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAutoMapper();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Title = ".NET Core API",
                    Version = "v1",
                    Contact = new Contact
                    {
                        Name = "Paweł Garbacik",
                        Email = "pawel.garbacik@takes-care.com",
                        Url = string.Empty
                    }
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            services.AddCors();
            services.AddMvc();

            services.AddDbContext<AuthDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("ApiDbConnection"))
            );

            services.AddDbContext<AuthDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("AppDbConnection"))
            );
            services
                .AddIdentity<ApiUser, IdentityRole>(options =>
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequiredLength = 8;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;

                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.AllowedForNewUsers = true;

                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<AuthDbContext>()
                .AddDefaultTokenProviders();

            var apiSettingsSection = Configuration.GetSection("ApiConfig");
            services.Configure<ApiConfig>(apiSettingsSection);
            ApiConfig = apiSettingsSection.Get<ApiConfig>();


            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(cfg =>
                {
                    cfg.RequireHttpsMetadata = false;
                    cfg.SaveToken = true;
                    cfg.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = ApiConfig.JwtIssuer,
                        ValidateIssuer = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiConfig.SecurityKey)),
                        ClockSkew = TimeSpan.Zero
                    };
                    cfg.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = ctx =>
                        {
                            var userService = ctx.HttpContext.RequestServices.GetRequiredService<UserManager<ApiUser>>();
                            var user = userService.FindByIdAsync(ctx.Principal.Identity.Name).Result;

                            if (user == null || !user.Verified || !user.EmailConfirmed)
                            {
                                ctx.Fail("Unauthorized");
                            }

                            return Task.CompletedTask;
                        }
                    };
                });
           
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseAuthentication();
            app.UseMvc();
            app.UseCors(builder =>
                builder.WithOrigins("http://localhost")
                    .AllowAnyMethod()
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
            );

            CreateUserRolesAsync(serviceProvider).Wait();
        }

        private async Task CreateUserRolesAsync(IServiceProvider serviceProvider)
        {
            if (ApiConfig == null || ApiConfig.Roles == null) return;

            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            foreach (var apiRole in ApiConfig.Roles)
            {
                var roleExists = await roleManager.RoleExistsAsync(apiRole);
                if (!roleExists)
                {
                    await roleManager.CreateAsync(new IdentityRole(apiRole));
                }
            }
        }
    }
}
