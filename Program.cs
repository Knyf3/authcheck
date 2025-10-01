
using authcheck.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace authcheck
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<AppDBContext>(option =>
            {
                option.UseSqlServer(builder.Configuration.GetConnectionString("defaultConnection"));
                //option.UseInMemoryDatabase("AuthDb");
            });

            // Add services to the container.
            builder.Services.AddAuthorization();
            builder.Services.AddIdentityApiEndpoints<IdentityUser>(options =>
            {
                options.SignIn.RequireConfirmedEmail = false;  // Disable email confirmation requirement
                //options.User.RequireUniqueEmail = false;       // Email doesn't need to be unique
                
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+"; // Allowed characters in username
                
            })
                .AddRoles<IdentityRole>() // Add role
                .AddEntityFrameworkStores<AppDBContext>();


            var jwtKey = builder.Configuration["Jwt:Key"] ?? "your_secret_key_here";
            var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "authcheck";
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtIssuer,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                    };
                });



            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo()
                {
                    Title = "Auth Demo",
                    Version = "v1"

                });
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme()
                {
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Please Enter the Token",
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "bearer"
                });

                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        []
                    }
                });
            }
            );


            var app = builder.Build();

            app.MapIdentityApi<IdentityUser>();
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();

           

            app.MapControllers();

            // Add this seed data method before app.Run()
            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

                // Create Admin role if it doesn't exist
                if (!await roleManager.RoleExistsAsync("Admin"))
                {
                    await roleManager.CreateAsync(new IdentityRole("Admin"));
                }

                // Create Operator role if it doesn't exist
                if (!await roleManager.RoleExistsAsync("Operator"))
                {
                    await roleManager.CreateAsync(new IdentityRole("Operator"));
                }

                //checking if there is user in the database
                bool hasAnyUser = await userManager.Users.AnyAsync();
                if (!hasAnyUser)
                {
                    // Create default admin user if it doesn't exist
                    var adminUser = await userManager.FindByNameAsync("admin");
                    if (adminUser == null)
                    {
                        var admin = new IdentityUser
                        {
                            UserName = "admin",
                            Email = "admin@example.com",
                            EmailConfirmed = true
                        };

                        var result = await userManager.CreateAsync(admin, "Admin1!");
                        if (result.Succeeded)
                        {
                            // Assign Admin role to the admin user
                            await userManager.AddToRoleAsync(admin, "Admin");
                        }
                    }
                }

               
            }


            app.Run();
        }
    }
}
