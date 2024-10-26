using Microsoft.EntityFrameworkCore;
using Repositories.Context;
using Repositories.Repositories;
using BusinessObjects.Contracts.Repositories;
using Services.Interfaces;
using Services.Services;
using BusinessObjects.Contracts.UnitOfWorks;
using Repositories.UnitOfWorks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Repositories.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using PRN231.ExploreNow.BusinessObject.Entities;
using BusinessObjects.Entities;
using Serilog.Sinks.Elasticsearch;
using Serilog;
using System.Reflection;
using Nest;
using Serilog.Exceptions;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

var builder = WebApplication.CreateBuilder(args);

#region Configure DbContext
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
	options.Password.RequireDigit = true;
	options.Password.RequireLowercase = true;
	options.Password.RequireUppercase = true;
	options.Password.RequireNonAlphanumeric = true;
	options.Password.RequiredLength = 8;
})
	.AddEntityFrameworkStores<ApplicationDbContext>()
	.AddDefaultTokenProviders();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
	var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
	var serverVersion = new MySqlServerVersion(new Version(8, 0, 28));
	options.UseMySql(connectionString, serverVersion)
		.LogTo(Console.WriteLine, LogLevel.Information)
		.EnableSensitiveDataLogging()
		.EnableDetailedErrors();
});
#endregion

#region Configure JWT Authentication 
var jwtSettings = builder.Configuration.GetSection("JWT");

builder.Services
	.AddAuthentication(options =>
	{
		options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
		options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
	})
	.AddJwtBearer(options =>
	{
		options.SaveToken = true;
		options.RequireHttpsMetadata = false;
		options.TokenValidationParameters = new TokenValidationParameters()
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = jwtSettings["ValidIssuer"],
			ValidAudience = jwtSettings["ValidAudience"],
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"])),
		};
	});

builder.Services.Configure<JwtBearerOptions>(options =>
{
	options.TokenValidationParameters = new TokenValidationParameters
	{
		ValidateIssuer = true,
		ValidateAudience = true,
		ValidateLifetime = true,
		ValidateIssuerSigningKey = true,
		ValidIssuer = jwtSettings["ValidIssuer"],
		ValidAudience = jwtSettings["ValidAudience"],
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"])),
	};
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
	options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
	{
		Name = "Authorization",
		In = Microsoft.OpenApi.Models.ParameterLocation.Header,
		Description = "Please enter your token with this format: ''Bearer YOUR_TOKEN''",
		Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
		BearerFormat = "JWT",
		Scheme = "bearer"
	});
	options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Name = "Bearer",
				In = ParameterLocation.Header,
				Reference = new OpenApiReference
				{
					Id = "Bearer",
					Type = ReferenceType.SecurityScheme
				}
			},
			new List<string>()
		}
	});
	// Thêm endpoint chạy HTTPS
	options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
			Array.Empty<string>()
		}
	});
});

#endregion

#region Configure CORS
builder.Services.AddCors(p =>
	p.AddPolicy(
		"corspolicy",
		build =>
		{
			build
				.WithOrigins("https://localhost:7047", "http://localhost:5176")
				.AllowAnyMethod()
				.AllowAnyHeader()
				.AllowCredentials();
		}
	)
);
#endregion

#region Config ElasticSearch
builder.Services.AddSingleton<IElasticClient>(provider =>
{
	var configuration = provider.GetRequiredService<IConfiguration>();
	var settings = new ConnectionSettings(new Uri(configuration["ElasticConfiguration:Uri"]))
		.DefaultMappingFor<Product>(m => m.IndexName(configuration["ElasticConfiguration:Index"]))
		.DisableDirectStreaming()
		.BasicAuthentication(
			configuration["ElasticConfiguration:Username"],
			configuration["ElasticConfiguration:Password"]
		);

	// Create ElasticClient without specifying default index
	return new ElasticClient(settings);
});

void ConfigureLogging()
{
	var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
	var configuration = new ConfigurationBuilder()
		.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
		.AddJsonFile($"appsettings.{environment}.json", optional: true)
		.Build();

	Log.Logger = new LoggerConfiguration()
		.Enrich.FromLogContext()
		.Enrich.WithExceptionDetails()
		.WriteTo.Debug()
		.WriteTo.Console()
		.WriteTo.Elasticsearch(ConfigureElasticSink(configuration, environment))
		.Enrich.WithProperty("Environment", environment)
		.ReadFrom.Configuration(configuration)
		.CreateLogger();
}

ElasticsearchSinkOptions ConfigureElasticSink(IConfigurationRoot configuration, string environment)
{
	return new ElasticsearchSinkOptions(new Uri(configuration["ElasticConfiguration:Uri"]))
	{
		AutoRegisterTemplate = true,
		IndexFormat = $"{Assembly.GetExecutingAssembly().GetName().Name.ToLower().Replace(".", "-")}-{environment?.ToLower().Replace(".", "-")}-{DateTime.UtcNow:yyyy-MM}",
		ModifyConnectionSettings = conn =>
			conn.BasicAuthentication(
				configuration["ElasticConfiguration:Username"],
				configuration["ElasticConfiguration:Password"]
			)
	};
}
#endregion

#region Mapping Profile

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

#endregion

#region Add Scoped
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IJwtService, JwtService>();
#endregion

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseRouting();

app.UseCors("corspolicy");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Database initialization
using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	try
	{
		var context = services.GetRequiredService<ApplicationDbContext>();
		Console.WriteLine("Attempting to ensure database is created...");
		context.Database.EnsureCreated();
		Console.WriteLine("Database created successfully");
	}
	catch (Exception ex)
	{
		Console.WriteLine($"An error occurred while creating the database: {ex.Message}");
		Console.WriteLine($"Stack trace: {ex.StackTrace}");
	}
}

//app.Run();
string port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");
