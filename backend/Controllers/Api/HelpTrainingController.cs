using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Controllers.Api
{
    // DTO Classes
    public class HelpArticleDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string LastUpdated { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public int Order { get; set; }
        
        // Additional properties for help articles
        public int ReadingTime { get; set; }
        public List<string> Tags { get; set; } = new();
        
        // Legacy properties for backward compatibility
        public string Description { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
    }

    public class HelpArticleDetailDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string LastUpdated { get; set; } = string.Empty;
        public int EstimatedReadTime { get; set; }
        public string Difficulty { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }

    public class HelpCategoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public int Order { get; set; }
    }

    public class TrainingVideoDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty;
        public int Order { get; set; }
    }

    public class HelpFeedbackDto
    {
        public string ArticleId { get; set; } = string.Empty;
        public int Rating { get; set; } // 1-5 stars
        public bool Helpful { get; set; }
        public string? Comment { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }

    [ApiController]
    [Route("api/[controller]")]
    public class HelpTrainingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HelpTrainingController> _logger;

        public HelpTrainingController(ApplicationDbContext context, ILogger<HelpTrainingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("categories")]
        public async Task<ActionResult<List<HelpCategoryDto>>> GetHelpCategories()
        {
            try
            {
                var categories = await _context.HelpCategories
                    .Where(c => c.Status == "Active")
                    .OrderBy(c => c.Order)
                    .Select(c => new HelpCategoryDto
                    {
                        Id = c.CategoryId,
                        Name = c.Name,
                        Description = c.Description,
                        Icon = c.Icon,
                        Order = c.Order
                    })
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving help categories");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("articles")]
        public async Task<ActionResult<List<HelpArticleDto>>> GetHelpArticles([FromQuery] string? category = null)
        {
            try
            {
                var query = _context.HelpArticles
                    .Where(a => a.Status == "Published");

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(a => a.CategoryId == category);
                }

                var articles = await query
                    .OrderByDescending(a => a.Order)
                    .ThenBy(a => a.Title)
                    .Select(a => new HelpArticleDto
                    {
                        Id = a.ArticleId,
                        Title = a.Title,
                        Description = a.Description,
                        CategoryId = a.CategoryId,
                        ReadingTime = 5, // Default reading time
                        Order = a.Order,
                        Difficulty = "Beginner", // Default difficulty, can be added to entity later
                        Tags = new List<string>() // Default tags, can be added to entity later
                    })
                    .ToListAsync();

                return Ok(articles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving help articles");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("articles/{id}")]
        public async Task<ActionResult<HelpArticleDetailDto>> GetHelpArticle(string id)
        {
            try
            {
                // Get article with parsing outside expression tree
                var articleEntity = await _context.HelpArticles
                    .Where(a => a.ArticleId == id && a.Status == "Published")
                    .FirstOrDefaultAsync();

                if (articleEntity == null)
                {
                    return NotFound(new { error = "Article not found" });
                }

                int.TryParse(articleEntity.ReadingTime = "5", out var minutes);
                
                var article = new HelpArticleDetailDto
                {
                    Id = articleEntity.ArticleId,
                    Title = articleEntity.Title,
                    CategoryId = articleEntity.CategoryId,
                    Content = articleEntity.Content,
                    LastUpdated = (articleEntity.LastUpdated ?? articleEntity.UpdatedAt).ToString("yyyy-MM-dd HH:mm:ss"),
                    EstimatedReadTime = minutes > 0 ? minutes : 5,
                    Difficulty = "Beginner", // Default difficulty, can be added to entity later
                    Tags = new List<string>() // Default tags, can be added to entity later
                };

                if (article == null)
                {
                    return NotFound(new { error = "Article not found" });
                }

                // Increment view count
                var articleForUpdate = await _context.HelpArticles
                    .FirstOrDefaultAsync(a => a.ArticleId == id);
                if (articleForUpdate != null)
                {
                    articleForUpdate.ViewCount += 1;
                    await _context.SaveChangesAsync();
                }

                return Ok(article);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving help article: {ArticleId}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<HelpArticleDto>>> SearchHelpArticles([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest(new { error = "Search query is required" });
                }

                var articles = GetPredefinedArticles();
                var searchResults = articles
                    .Where(a => a.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                               a.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return Ok(searchResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching help articles with query: {Query}", query);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("videos")]
        public async Task<ActionResult<List<TrainingVideoDto>>> GetTrainingVideos([FromQuery] string? category = null)
        {
            try
            {
                var videos = GetPredefinedVideos();

                if (!string.IsNullOrEmpty(category))
                {
                    videos = videos.Where(v => v.CategoryId == category).ToList();
                }

                return Ok(videos.OrderByDescending(v => v.Order).ThenBy(v => v.Title).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving training videos");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("feedback")]
        public async Task<ActionResult> SubmitHelpFeedback([FromBody] HelpFeedbackDto feedback)
        {
            try
            {
                // Get current user context
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                // Create feedback entity
                var feedbackEntity = new HelpFeedback
                {
                    ArticleId = feedback.ArticleId,
                    UserId = userId,
                    TenantId = tenantId,
                    Rating = feedback.Rating,
                    Helpful = feedback.Helpful,
                    Comment = feedback.Comment,
                    CreatedAt = DateTime.UtcNow
                };

                // Save to database
                await _context.HelpFeedbacks.AddAsync(feedbackEntity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Help feedback received: ArticleId={ArticleId}, Rating={Rating}, Helpful={Helpful}", 
                    feedback.ArticleId, feedback.Rating, feedback.Helpful);

                return Ok(new { message = "Feedback submitted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting help feedback");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        private List<HelpArticleDto> GetPredefinedArticles()
        {
            return new List<HelpArticleDto>
            {
                new HelpArticleDto
                {
                    Id = "quick-start-guide",
                    Title = "Quick Start Guide",
                    Description = "Get up and running with UMI Health POS in minutes",
                    CategoryId = "getting-started",
                    Order = 1,
                    Difficulty = "Beginner"
                },
                new HelpArticleDto
                {
                    Id = "user-account-setup",
                    Title = "Setting Up Your User Account",
                    Description = "Configure your profile and preferences",
                    CategoryId = "getting-started",
                    Order = 2,
                    Difficulty = "Beginner"
                },
                new HelpArticleDto
                {
                    Id = "adding-products",
                    Title = "Adding Products to Inventory",
                    Description = "Learn how to add and manage products",
                    CategoryId = "inventory-management",
                    Order = 1,
                    Difficulty = "Beginner"
                },
                new HelpArticleDto
                {
                    Id = "stock-management",
                    Title = "Stock Level Management",
                    Description = "Monitor and maintain optimal stock levels",
                    CategoryId = "inventory-management",
                    Order = 2,
                    Difficulty = "Intermediate"
                },
                new HelpArticleDto
                {
                    Id = "creating-prescriptions",
                    Title = "Creating Prescriptions",
                    Description = "Step-by-step guide to prescription creation",
                    CategoryId = "prescriptions",
                    Order = 1,
                    Difficulty = "Intermediate"
                },
                new HelpArticleDto
                {
                    Id = "zambia-drug-regulations",
                    Title = "Zambia Drug Regulations",
                    Description = "Understanding local pharmacy regulations",
                    CategoryId = "compliance",
                    Order = 1,
                    Difficulty = "Advanced"
                }
            };
        }

        private string GetArticleContent(string articleId)
        {
            return articleId switch
            {
                "quick-start-guide" => @"
# Quick Start Guide

Welcome to UMI Health POS! This guide will help you get started quickly.

## Step 1: Login
1. Open your web browser and navigate to the UMI Health POS URL
2. Enter your username and password
3. Select your branch if applicable
4. Click 'Login'

## Step 2: Dashboard Overview
- **Sales Overview**: View today's sales and transactions
- **Low Stock Alerts**: Products that need reordering
- **Recent Prescriptions**: Latest prescription activity
- **Quick Actions**: Common tasks shortcuts

## Step 3: Basic Operations
- **Add Products**: Navigate to Inventory → Add Product
- **Process Sales**: Point of Sale → New Sale
- **Create Prescriptions**: Prescriptions → New Prescription

## Step 4: Getting Help
- Press F1 anywhere in the application
- Use the Help menu in the top navigation
- Contact support at support@umihealth.com

Need more help? Check out our video tutorials!",
                
                "user-account-setup" => @"
# Setting Up Your User Account

## Profile Configuration

### Personal Information
1. Go to Account → Profile
2. Update your name, email, and phone number
3. Add your professional license information
4. Set your preferred language

### Security Settings
1. Enable Two-Factor Authentication (recommended)
2. Set up password recovery options
3. Configure session timeout preferences

### Notification Preferences
1. Choose which notifications you want to receive
2. Set email notification frequency
3. Configure in-app notification settings

## Branch Access
If you work at multiple branches:
1. Go to Account → Branch Access
2. Select your primary branch
3. Request access to additional branches if needed

## Next Steps
Once your account is set up, you can:
- Start managing inventory
- Process sales and prescriptions
- Generate reports
- Manage other users (if admin)",
                
                "adding-products" => @"
# Adding Products to Inventory

## Before You Start
Make sure you have:
- Product details (name, generic name, brand)
- Zambia registration number
- Supplier information
- Pricing details

## Step-by-Step Guide

### 1. Navigate to Inventory
- Go to Inventory → Add Product
- Or click 'Add Product' from the inventory dashboard

### 2. Basic Information
- **Product Name**: Commercial name (e.g., Panadol)
- **Generic Name**: Active ingredient (e.g., Paracetamol)
- **Brand**: Manufacturer brand
- **Category**: Select appropriate drug category

### 3. Zambia Compliance
- **Zambia REG Number**: Enter ZAMRA registration number
- **License Number**: Pharmacy license requirement
- **Storage Requirements**: Special storage conditions

### 4. Stock Information
- **Initial Quantity**: Current stock on hand
- **Unit Price**: Cost price per unit
- **Selling Price**: Retail price per unit
- **Reorder Level**: When to reorder

### 5. Save Product
- Review all information
- Click 'Save Product'
- Product will appear in inventory list

## Tips
- Use barcode scanner for faster entry
- Batch import multiple products via CSV
- Set up automatic reorder notifications",
                
                _ => @"# Article Content

This article is currently being updated. Please check back soon for the complete content.

For immediate assistance, please:
- Contact our support team
- Check our video tutorials
- Browse related articles in the help section"
            };
        }

        private List<string> GetArticleTags(string articleId)
        {
            return articleId switch
            {
                "quick-start-guide" => new List<string> { "beginner", "setup", "getting-started" },
                "user-account-setup" => new List<string> { "account", "profile", "security" },
                "adding-products" => new List<string> { "inventory", "products", "stock" },
                "stock-management" => new List<string> { "inventory", "stock", "reorder" },
                "creating-prescriptions" => new List<string> { "prescriptions", "clinical", "patient-care" },
                "zambia-drug-regulations" => new List<string> { "compliance", "zambia", "regulations", "zamra" },
                _ => new List<string>()
            };
        }

        private List<TrainingVideoDto> GetPredefinedVideos()
        {
            return new List<TrainingVideoDto>
            {
                new TrainingVideoDto
                {
                    Id = "intro-video",
                    Title = "Introduction to UMI Health POS",
                    Description = "Complete overview of the system",
                    CategoryId = "getting-started",
                    Duration = "5:30",
                    ThumbnailUrl = "/images/videos/intro-thumb.jpg",
                    VideoUrl = "#", // Would be actual video URL
                    Order = 1
                },
                new TrainingVideoDto
                {
                    Id = "inventory-basics",
                    Title = "Inventory Management Basics",
                    Description = "Learn the fundamentals of inventory management",
                    CategoryId = "inventory-management",
                    Duration = "8:45",
                    ThumbnailUrl = "/images/videos/inventory-thumb.jpg",
                    VideoUrl = "#",
                    Order = 1
                },
                new TrainingVideoDto
                {
                    Id = "prescription-workflow",
                    Title = "Prescription Workflow",
                    Description = "Complete prescription processing workflow",
                    CategoryId = "prescriptions",
                    Duration = "12:20",
                    ThumbnailUrl = "/images/videos/prescription-thumb.jpg",
                    VideoUrl = "#",
                    Order = 1
                }
            };
        }

        // Helper Methods
        private string? GetCurrentUserId()
        {
            return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }

        private string? GetCurrentTenantId()
        {
            return User.FindFirst("TenantId")?.Value;
        }
    }
}
