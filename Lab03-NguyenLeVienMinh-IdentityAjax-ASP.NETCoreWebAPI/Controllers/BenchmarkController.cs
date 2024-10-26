using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using BusinessObjects.Entities;
using BusinessObjects.OtherObjects;
using Nest;
using Repositories.Context;
using Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using PRN231.ExploreNow.BusinessObject.Entities;

[ApiController]
[Route("api/benchmarks")]
public class BenchmarkController : ControllerBase
{
	private readonly IProductService _productService;
	private readonly ApplicationDbContext _context;
	private readonly IElasticClient _elasticClient;
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly RoleManager<IdentityRole> _roleManager;

	public BenchmarkController(
		IProductService productService,
		ApplicationDbContext context,
		IElasticClient elasticClient,
		UserManager<ApplicationUser> userManager,
		RoleManager<IdentityRole> roleManager)
	{
		_productService = productService;
		_context = context;
		_elasticClient = elasticClient;
		_userManager = userManager;
		_roleManager = roleManager;
	}

	[HttpGet("compare")]
	public async Task<IActionResult> ComparePerfomance([FromQuery] int iterations = 10)
	{
		try
		{
			await EnsureTestData(10000);

			var dbTimes = new List<long>();
			var elasticTimes = new List<long>();

			// Warm up
			await _productService.GetProducts();
			await _productService.SearchProductAsync(null, null, null);
			await Task.Delay(1000); // Đợi 1 giây sau warm up

			// Chạy test cho từng loại query
			for (int i = 0; i < iterations; i++)
			{
				// Basic Query Test
				var dbStopwatch = Stopwatch.StartNew();
				var dbResult = await _productService.GetProducts();
				dbStopwatch.Stop();
				dbTimes.Add(dbStopwatch.ElapsedMilliseconds);

				var elasticStopwatch = Stopwatch.StartNew();
				var esResult = await _productService.SearchProductAsync(null, null, null);
				elasticStopwatch.Stop();
				elasticTimes.Add(elasticStopwatch.ElapsedMilliseconds);

				await Task.Delay(200); // Tăng delay giữa các lần test
			}

			// Phân tích kết quả cơ bản
			var basicResult = new BenchmarkResult
			{
				DatasetSize = 10000,
				DatabaseStats = CalculateStats(dbTimes),
				ElasticsearchStats = CalculateStats(elasticTimes)
			};

			// Test truy vấn phức tạp
			var complexResult = await TestComplexQueries(iterations);

			// Chi tiết phân tích từng record
			var recordAnalysis = await AnalyzeIndividualRecords();

			return Ok(new
			{
				BasicQueries = basicResult,
				ComplexQueries = complexResult,
				RecordAnalysis = recordAnalysis,
				Summary = GenerateDetailedSummary(basicResult, complexResult, recordAnalysis)
			});
		}
		catch (Exception ex)
		{
			return StatusCode(500, $"Benchmark failed: {ex.Message}");
		}
	}

	private async Task<object> AnalyzeIndividualRecords()
	{
		// Phân tích chi tiết từng record
		var dbProducts = await _context.Products.Take(100).ToListAsync();
		var esProducts = await _elasticClient.SearchAsync<Product>(s => s
			.Size(100)
			.Sort(sort => sort.Ascending(f => f.ProductId))
		);

		// Chuyển đổi sang double để tính toán
		var dbAveragePrice = (double)dbProducts.Average(p => p.UnitPrice);
		var esAveragePrice = (double)esProducts.Documents.Average(p => p.UnitPrice);
		var dbAverageStock = (double)dbProducts.Average(p => p.UnitsInStock);
		var esAverageStock = (double)esProducts.Documents.Average(p => p.UnitsInStock);

		var analysis = new
		{
			DatabaseRecords = new
			{
				TotalCount = dbProducts.Count,
				AveragePrice = dbAveragePrice,
				PriceRange = new
				{
					Min = (double)dbProducts.Min(p => p.UnitPrice),
					Max = (double)dbProducts.Max(p => p.UnitPrice)
				},
				StockAnalysis = new
				{
					TotalStock = dbProducts.Sum(p => p.UnitsInStock),
					AverageStock = dbAverageStock
				}
			},
			ElasticsearchRecords = new
			{
				TotalCount = esProducts.Documents.Count,
				AveragePrice = esAveragePrice,
				PriceRange = new
				{
					Min = (double)esProducts.Documents.Min(p => p.UnitPrice),
					Max = (double)esProducts.Documents.Max(p => p.UnitPrice)
				},
				StockAnalysis = new
				{
					TotalStock = esProducts.Documents.Sum(p => p.UnitsInStock),
					AverageStock = esAverageStock
				}
			},
			Comparison = new
			{
				DataConsistency = dbProducts.Count == esProducts.Documents.Count,
				PriceConsistency = Math.Abs(dbAveragePrice - esAveragePrice) < 0.01,
				StockConsistency = Math.Abs(dbAverageStock - esAverageStock) < 0.01
			}
		};

		return analysis;
	}

	private async Task<BenchmarkResult> TestComplexQueries(int iterations)
	{
		var complexDbTimes = new List<long>();
		var complexElasticTimes = new List<long>();

		// Test complex search with multiple conditions
		for (int i = 0; i < iterations; i++)
		{
			// Database complex query
			var dbStopwatch = Stopwatch.StartNew();
			await _context.Products
				.Include(p => p.Category)
				.Where(p => p.UnitPrice >= 10 && p.UnitPrice <= 100)
				.Where(p => p.ProductName.Contains("Test"))
				.OrderBy(p => p.UnitPrice)
				.ToListAsync();
			dbStopwatch.Stop();
			complexDbTimes.Add(dbStopwatch.ElapsedMilliseconds);

			// Elasticsearch complex query
			var elasticStopwatch = Stopwatch.StartNew();
			await _productService.SearchProductAsync("Test", 10, 100);
			elasticStopwatch.Stop();
			complexElasticTimes.Add(elasticStopwatch.ElapsedMilliseconds);
		}

		return new BenchmarkResult
		{
			DatasetSize = await _context.Products.CountAsync(),
			DatabaseStats = CalculateStats(complexDbTimes),
			ElasticsearchStats = CalculateStats(complexElasticTimes),
			QueryType = "Complex Search"
		};
	}

	private Statistics CalculateStats(List<long> times)
	{
		return new Statistics
		{
			AverageMs = times.Average(),
			MedianMs = CalculateMedian(times),
			MinMs = times.Min(),
			MaxMs = times.Max(),
			StandardDeviation = CalculateStandardDeviation(times)
		};
	}

	private double CalculateMedian(List<long> times)
	{
		var sortedTimes = times.OrderBy(t => t).ToList();
		int middle = sortedTimes.Count / 2;
		if (sortedTimes.Count % 2 == 0)
			return (sortedTimes[middle - 1] + sortedTimes[middle]) / 2.0;
		return sortedTimes[middle];
	}

	private double CalculateStandardDeviation(List<long> times)
	{
		double average = times.Average();
		double sumOfSquaresOfDifferences = times.Select(val => (val - average) * (val - average)).Sum();
		return Math.Sqrt(sumOfSquaresOfDifferences / times.Count);
	}

	private string GenerateDetailedSummary(
		BenchmarkResult basicResult,
		BenchmarkResult complexResult,
		object recordAnalysis)
	{
		var sb = new StringBuilder();
		sb.AppendLine("Detailed Performance Analysis for 10,000 Records");
		sb.AppendLine("==============================================");

		// Basic Query Performance
		double basicSpeedup = basicResult.DatabaseStats.AverageMs / basicResult.ElasticsearchStats.AverageMs;
		sb.AppendLine("\nBasic Query Performance:");
		sb.AppendLine($"Database Avg: {basicResult.DatabaseStats.AverageMs:F2}ms");
		sb.AppendLine($"Elasticsearch Avg: {basicResult.ElasticsearchStats.AverageMs:F2}ms");
		sb.AppendLine($"Basic Query Speedup: {basicSpeedup:F2}x");

		// Complex Query Performance
		double complexSpeedup = complexResult.DatabaseStats.AverageMs / complexResult.ElasticsearchStats.AverageMs;
		sb.AppendLine("\nComplex Query Performance:");
		sb.AppendLine($"Database Avg: {complexResult.DatabaseStats.AverageMs:F2}ms");
		sb.AppendLine($"Elasticsearch Avg: {complexResult.ElasticsearchStats.AverageMs:F2}ms");
		sb.AppendLine($"Complex Query Speedup: {complexSpeedup:F2}x");

		// Performance Consistency
		sb.AppendLine("\nPerformance Consistency:");
		sb.AppendLine($"Database Variation: {basicResult.DatabaseStats.StandardDeviation:F2}ms");
		sb.AppendLine($"Elasticsearch Variation: {basicResult.ElasticsearchStats.StandardDeviation:F2}ms");

		return sb.ToString();
	}

	private async Task<string> EnsureAdminUser()
	{
		// Kiểm tra và tạo ADMIN role nếu chưa có
		if (!await _roleManager.RoleExistsAsync("ADMIN"))
		{
			await _roleManager.CreateAsync(new IdentityRole("ADMIN"));
		}

		// Thông tin admin mặc định
		const string adminEmail = "admin@gmail.com";
		const string adminPassword = "@String123";

		// Tìm admin user
		var adminUser = await _userManager.FindByEmailAsync(adminEmail);

		if (adminUser == null)
		{
			// Tạo admin user mới nếu chưa tồn tại
			adminUser = new ApplicationUser
			{
				UserName = "admin",
				Email = adminEmail,
				EmailConfirmed = true,
				FirstName = "admin",
				LastName = "admin",
				isActived = true
			};

			var result = await _userManager.CreateAsync(adminUser, adminPassword);
			if (!result.Succeeded)
			{
				throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
			}

			// Gán role ADMIN
			await _userManager.AddToRoleAsync(adminUser, "ADMIN");
		}

		return adminUser.Id;
	}

	private async Task EnsureTestData(int size)
	{
		var currentCount = await _context.Products.CountAsync();
		if (currentCount < size)
		{
			// Đảm bảo có admin user và lấy ID
			var adminUserId = await EnsureAdminUser();

			// Add more test data if needed
			var productsToAdd = new List<Product>();
			var currentTime = DateTime.UtcNow;

			for (int i = currentCount; i < size; i++)
			{
				productsToAdd.Add(new Product
				{
					ProductName = $"Test Product {i}",
					UnitPrice = Random.Shared.Next(1, 1000),
					CategoryId = 1,
					UnitsInStock = Random.Shared.Next(1, 100),
					CreatedBy = "admin",
					CreatedDate = currentTime,
					UserId = adminUserId
				});

				// Bulk insert sau mỗi 1000 records để tránh memory issues
				if (productsToAdd.Count >= 1000 || i == size - 1)
				{
					await _context.Products.AddRangeAsync(productsToAdd);
					await _context.SaveChangesAsync();
					productsToAdd.Clear();
				}
			}

			// Index new data in Elasticsearch
			await _productService.CreateProductDocumentsAsync();
		}
	}
}