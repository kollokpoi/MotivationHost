using System.Globalization;
using FluentScheduler;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Motivation.Controllers.MobileApi;
using Motivation.Core.Interfaces;
using Motivation.Core.Services;
using Motivation.Data;
using Motivation.Data.Repositories;
using Motivation.Models;
using Motivation.Options;

namespace Motivation
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var postgresOptions = builder.Configuration.Get<PostgresOptions>();
            if (postgresOptions == null)
            {
                throw new Exception("Can't get AppOptions");
            }

            var server = postgresOptions.PostgresServer;
            var port = postgresOptions.PostgresPort;
            var user = postgresOptions.PostgresUser;
            var password = postgresOptions.PostgresPassword;

            var appConnectionString =
                $"Server={server};Port={port};Username={user};Password={password};Database={postgresOptions.AppDatabaseName};";
            var identityConnectionString =
                $"Server={server};Port={port};Username={user};Password={password};Database={postgresOptions.IdentityDatabaseName};";

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(appConnectionString)
            );
            builder.Services.AddDbContext<AppIdentityDbContext>(options =>
                options.UseNpgsql(identityConnectionString)
            );
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder
                .Services.AddIdentity<IdentityUser, IdentityRole>(options =>
                {
                    options.Password.RequiredLength = 6;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireDigit = false;
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<AppIdentityDbContext>()
                .AddDefaultTokenProviders();

            builder
                .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = AuthOptions.Issuer,
                        ValidateAudience = true,
                        ValidAudience = AuthOptions.Audience,
                        ValidateLifetime = false,
                        IssuerSigningKey = AuthOptions.GetSymmetricAccessSecurityKey(),
                        ValidateIssuerSigningKey = true,
                    };
                });

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.SlidingExpiration = true;
            });

            builder.Services.AddMvc();
            builder.Services.AddApiVersioning();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddScoped<IRepository<Department>, SyncEnabledDepartmentsRepository>();
            builder.Services.AddScoped<ISyncEnabledDepartmentsRepository, SyncEnabledDepartmentsRepository>();
            builder.Services.AddScoped<IRepository<Position>, PositionsRepository>();
            builder.Services.AddScoped<IRepository<Qualification>, QualificationsRepository>();
            builder.Services.AddScoped<IRepository<Rank>, RanksRepository>();
            builder.Services.AddScoped<IRepository<Penalty>, PenaltiesRepository>();
            builder.Services.AddScoped<IRepository<EmployeePenalty>, EmployeePenaltiesRepository>();
            builder.Services.AddScoped<IEmployeesRepository, EmployeesRepository>();
            builder.Services.AddScoped<IRepository<EmployeeTask>, EmployeeTasksRepository>();
            builder.Services.AddScoped<IRepository<Shift>, ShiftsRepository>();
            builder.Services.AddScoped<IRepository<ShiftRule>, ShiftRulesRepository>();
            builder.Services.AddScoped<IRepository<PointOfInterest>, PointsOfInterestRepository>();
            builder.Services.AddScoped<ISalaryCalculator, SalaryCalculator>();
            builder.Services.AddScoped<IRepository<ScoreSheet>, ScoreSheetsRepository>();
            builder.Services.AddScoped<IRepository<Comment>, CommentsRepository>();
            builder.Services.AddScoped<IRepository<Bonus>, BonusRepository>();
            builder.Services.AddScoped<IRepository<BonusGradation>, BonusGradationRepository>();
            builder.Services.AddScoped<IRepository<EmployeeBonus>, EmployeeBonusRepository>();
            builder.Services.AddScoped<IEfficiencyCalculator, EfficeincyCalculator>();
            builder.Services.AddScoped<IDepartmentGetter, DepartmentGetter>();
            builder.Services.AddScoped<IScoreSheetGenerator, IScoreSheetGenerator>();

            // Bitrix Integration
            builder.Services.AddHttpClient<BitrixSyncService>();
            builder.Services.AddScoped<IBitrixSyncService, BitrixSyncService>();
            builder.Services.AddScoped<BitrixTasksRepository, BitrixTasksRepository>();
            builder.Services.AddScoped<BitrixTimemanRepository, BitrixTimemanRepository>();
            
            // Sync-enabled repositories for Bitrix24 integration
            builder.Services.AddScoped<IRepository<Department>, SyncEnabledDepartmentsRepository>();
            builder.Services.AddScoped<ISyncEnabledDepartmentsRepository, SyncEnabledDepartmentsRepository>();
            builder.Services.AddScoped<IRepository<Employee>, SyncEnabledEmployeesRepository>();
            builder.Services.AddScoped<ISyncEnabledEmployeesRepository, SyncEnabledEmployeesRepository>();
            builder.Services.AddScoped<IRepository<EmployeeTask>, SyncEnabledTasksRepository>();
            builder.Services.AddScoped<ISyncEnabledTasksRepository, SyncEnabledTasksRepository>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseStatusCodePages();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseCors(p =>
            {
                p.WithOrigins(
                    [
                        "https://bg59.online",
                        "https://background-dev.bitrix24.ru",
                        "https://vedernikov.bitrix24.ru",
                    ]
                );
                p.AllowAnyHeader();
                p.AllowAnyMethod();
            });

            app.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}"
                )
                .RequireAuthorization();

            var photoFolderPath = $"{builder.Environment.ContentRootPath}/Photos";
            var employeePhotoFolderPath = $"{builder.Environment.ContentRootPath}/Photos/Employees";
            var commentsPhotoFolderPath = $"{builder.Environment.ContentRootPath}/Photos/Comments";
            if (!Directory.Exists(photoFolderPath))
                Directory.CreateDirectory(photoFolderPath);
            if (!Directory.Exists(employeePhotoFolderPath))
                Directory.CreateDirectory(photoFolderPath);
            if (!Directory.Exists(commentsPhotoFolderPath))
                Directory.CreateDirectory(photoFolderPath);

            app.UseStaticFiles(
                new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(
                        Path.Combine(builder.Environment.ContentRootPath, "Photos")
                    ),
                    RequestPath = "/Photos",
                }
            );

            app.MapRazorPages();

            DoMigrations(app);
            SetupCulture();

            JobManager.Initialize();
            JobManager.AddJob(
                () => GenerateScoresheetsTask(app),
                s => s.ToRunEvery(1).Months().On(1).At(12, 00)
            );

            app.Run();
        }

        private static void DoMigrations(WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                using (var context = services.GetRequiredService<AppIdentityDbContext>())
                {
                    if (context.Database.GetPendingMigrations().Any())
                        context.Database.Migrate();
                }

                using (var context = services.GetRequiredService<ApplicationDbContext>())
                {
                    if (context.Database.GetPendingMigrations().Any())
                        context.Database.Migrate();
                }
            }
        }

        private static void SetupCulture()
        {
            var culture = new CultureInfo("ru-RU");
            culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.NumberFormat.CurrencyDecimalSeparator = ".";

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }

        private static void GenerateScoresheetsTask(WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var month = DateTime.Now.AddMonths(-1).Month;
                var year = DateTime.Now.AddMonths(-1).Year;

                var services = scope.ServiceProvider;
                var scoreSheetGenerator = services.GetRequiredService<IScoreSheetGenerator>();
                var employeesRepository = services.GetRequiredService<IEmployeesRepository>();

                foreach (var employee in employeesRepository.Entries.ToList())
                {
                    var task = scoreSheetGenerator.CreateForEmployee(employee, year, month);
                    task.Wait();
                }
            }
        }
    }
}
