// using ClothingWebApp.Models;
// using Microsoft.EntityFrameworkCore;
// using System.Globalization;
// using System.Text;

// namespace ClothingWebApp.Data
// {
    
//     /// Provides methods to seed initial data into the database
    
//     public static class DataSeeder
//     {
        
//         /// Seeds the Categories table with initial data
      
//         public static async Task SeedCategoriesAsync(ApplicationDbContext context)
//         {
//             if (await context.Categories.AnyAsync())
//             {
//                 Console.WriteLine("Categories already exist in database.");
//                 return; 
//             }

//             Console.WriteLine("Seeding categories...");
//             var categories = new List<Category>
//             {
//                 new Category { CategoryId = 1, Name = "BAGS", Description = "Stylish bags and accessories", Products = new List<Product>() },
//                 new Category { CategoryId = 2, Name = "BLAZERS", Description = "Elegant blazers for all occasions", Products = new List<Product>() },
//                 new Category { CategoryId = 3, Name = "DRESSES/JUMPSUITS", Description = "Beautiful dresses and jumpsuits", Products = new List<Product>() },
//                 new Category { CategoryId = 4, Name = "JACKETS", Description = "Trendy jackets for all seasons", Products = new List<Product>() },
//                 new Category { CategoryId = 5, Name = "SHIRTS", Description = "Comfortable and stylish shirts", Products = new List<Product>() },
//                 new Category { CategoryId = 6, Name = "SHOES", Description = "Fashionable footwear collection", Products = new List<Product>() },
//                 new Category { CategoryId = 7, Name = "SWEATERS", Description = "Warm and cozy sweaters", Products = new List<Product>() },
//                 new Category { CategoryId = 8, Name = "TRENDING", Description = "Most popular items right now", Products = new List<Product>() },
//                 new Category { CategoryId = 9, Name = "WAISTCOATS", Description = "Elegant waistcoats", Products = new List<Product>() },
//                 new Category { CategoryId = 10, Name = "SKIRTS", Description = "Stylish skirts collection", Products = new List<Product>() },
//                 new Category { CategoryId = 11, Name = "T-SHIRT/TOPS", Description = "Comfortable t-shirts and tops", Products = new List<Product>() },
//             };

//             await context.Categories.AddRangeAsync(categories);
//             await context.SaveChangesAsync();
//             Console.WriteLine("Categories seeded successfully!");
//         }
        
       
//         /// Imports products from a CSV file into the database
       
//         public static async Task ImportProductsFromCsvAsync(ApplicationDbContext context, string csvFilePath)
//         {
//             // Check if products already exist
//             if (await context.Products.AnyAsync())
//             {
//                 Console.WriteLine("Products already exist in database.");
//                 return;
//             }
            
//             // Verify file exists
//             if (!File.Exists(csvFilePath))
//             {
//                 Console.WriteLine($"CSV file not found at: {csvFilePath}");
//                 return;
//             }
            
//             Console.WriteLine($"Importing products from: {csvFilePath}");
            
//             try
//             {
//                 // Read lines with file sharing enabled
//                 List<string> lines = new List<string>();
//                 using (var fileStream = new FileStream(csvFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
//                 using (var reader = new StreamReader(fileStream))
//                 {
//                     string? line;
//                     while ((line = await reader.ReadLineAsync()) != null)
//                     {
//                         lines.Add(line);
//                     }
//                 }
                
//                 if (lines.Count <= 1)
//                 {
//                     Console.WriteLine("CSV file is empty or contains only headers.");
//                     return;
//                 }
                
//                 Console.WriteLine($"CSV file contains {lines.Count} lines (including header)");
                
//                 // Process header to identify column positions
//                 string[] headers = lines[0].Split(',');
//                 var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                
//                 for (int i = 0; i < headers.Length; i++)
//                 {
//                     string headerName = headers[i]?.Trim() ?? string.Empty;
//                     if (!string.IsNullOrEmpty(headerName))
//                     {
//                         columnMap[headerName] = i;
//                     }
//                 }
                
//                 Console.WriteLine($"Found columns: {string.Join(", ", columnMap.Keys)}");
                
//                 // Process data rows
//                 var products = new List<Product>();
//                 int successCount = 0;
//                 int errorCount = 0;
                
//                 for (int i = 1; i < lines.Count; i++)
//                 {
//                     if (string.IsNullOrWhiteSpace(lines[i]))
//                         continue;
                        
//                     try
//                     {
//                         // Split the line, handling possible commas in quoted fields
//                         var values = SplitCsvLine(lines[i]);
                        
//                         // Parse product ID
//                         int productId = i; // Default to line number
//                         if (columnMap.TryGetValue("ProductId", out int productIdIdx) && 
//                             productIdIdx < values.Length && 
//                             !string.IsNullOrEmpty(values[productIdIdx]) && 
//                             int.TryParse(values[productIdIdx], out int parsedProductId))
//                         {
//                             productId = parsedProductId;
//                         }
                        
//                         // Parse category ID
//                         int categoryId = 1; // Default to first category
//                         if (columnMap.TryGetValue("CategoryId", out int categoryIdIdx) && 
//                             categoryIdIdx < values.Length && 
//                             !string.IsNullOrEmpty(values[categoryIdIdx]) && 
//                             int.TryParse(values[categoryIdIdx], out int parsedCategoryId))
//                         {
//                             categoryId = parsedCategoryId;
//                         }
//                         else if (columnMap.TryGetValue("Category", out int categoryNameIdx) && 
//                                 categoryNameIdx < values.Length && 
//                                 !string.IsNullOrEmpty(values[categoryNameIdx]))
//                         {
//                             // Try to map category name to ID
//                             string categoryName = values[categoryNameIdx].Trim().ToUpper();
//                             var category = await context.Categories
//                                 .FirstOrDefaultAsync(c => c.Name.ToUpper() == categoryName);
                                
//                             if (category != null)
//                             {
//                                 categoryId = category.CategoryId;
//                             }
//                         }
                        
//                         // Parse price
//                         decimal price = 29.99m; // Default price
//                         if (columnMap.TryGetValue("Price", out int priceIdx) && 
//                             priceIdx < values.Length && 
//                             !string.IsNullOrEmpty(values[priceIdx]))
//                         {
//                             string priceStr = values[priceIdx].Trim();
//                             if (!decimal.TryParse(priceStr, out price))
//                             {
//                                 // Try alternative parsing with different culture
//                                 if (!decimal.TryParse(priceStr, NumberStyles.Any, 
//                                     CultureInfo.InvariantCulture, out price))
//                                 {
//                                     price = 29.99m; // Use default if parsing fails
//                                 }
//                             }
//                         }
                        
//                         // Process image URL
//                         string imageUrl = "/images/products/no-image.jpg"; // Default image
//                         if (columnMap.TryGetValue("ImageUrl", out int imageUrlIdx) && 
//                             imageUrlIdx < values.Length)
//                         {
//                             string rawImagePath = values[imageUrlIdx]?.Trim() ?? string.Empty;
//                             imageUrl = ProcessImagePath(rawImagePath);
//                         }
                        
//                         // Get product name
//                         string name = $"Product {productId}"; // Default name
//                         if (columnMap.TryGetValue("Name", out int nameIdx) && 
//                             nameIdx < values.Length && 
//                             !string.IsNullOrEmpty(values[nameIdx]))
//                         {
//                             name = values[nameIdx].Trim();
//                         }
                        
//                         // Get product description
//                         string description = "No description available"; // Default description
//                         if (columnMap.TryGetValue("Description", out int descIdx) && 
//                             descIdx < values.Length && 
//                             !string.IsNullOrEmpty(values[descIdx]))
//                         {
//                             description = values[descIdx].Trim();
//                         }
                        
//                         // Get color
//                         string color = "Default"; // Default color
//                         if (columnMap.TryGetValue("Color", out int colorIdx) && 
//                             colorIdx < values.Length && 
//                             !string.IsNullOrEmpty(values[colorIdx]))
//                         {
//                             color = values[colorIdx].Trim();
//                         }
                        
//                         // Get size
//                         string size = "One Size"; // Default size
//                         if (columnMap.TryGetValue("Size", out int sizeIdx) && 
//                             sizeIdx < values.Length && 
//                             !string.IsNullOrEmpty(values[sizeIdx]))
//                         {
//                             size = values[sizeIdx].Trim();
//                         }
                        
//                         // Create the product
//                         var product = new Product
//                         {
//                             ProductId = productId,
//                             Name = name,
//                             Description = description,
//                             Price = price,
//                             Color = color,
//                             Size = size,
//                             ImageUrl = imageUrl,
//                             CategoryId = categoryId
//                         };
                        
//                         products.Add(product);
//                         successCount++;
                        
//                         // Log progress periodically
//                         if (i % 20 == 0 || i == lines.Count - 1)
//                         {
//                             Console.WriteLine($"Processed {i}/{lines.Count-1} products...");
//                         }
//                     }
//                     catch (Exception ex)
//                     {
//                         Console.WriteLine($"Error processing line {i+1}: {ex.Message}");
//                         errorCount++;
//                     }
//                 }
                
//                 // Save products to database in batches
//                 if (products.Any())
//                 {
//                     const int batchSize = 20; // Using smaller batches to avoid issues
//                     int batchCount = (products.Count + batchSize - 1) / batchSize; // Ceiling division
                    
//                     for (int i = 0; i < products.Count; i += batchSize)
//                     {
//                         var batch = products.Skip(i).Take(batchSize).ToList();
//                         await context.Products.AddRangeAsync(batch);
//                         await context.SaveChangesAsync();
//                         Console.WriteLine($"Saved batch {i/batchSize + 1}/{batchCount} ({batch.Count} products)");
//                     }
                    
//                     Console.WriteLine($"Summary: Successfully imported {successCount} products. Errors: {errorCount}");
//                 }
//                 else
//                 {
//                     Console.WriteLine("No products were imported. Check for errors above.");
//                 }
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine($"Critical error during CSV import: {ex.Message}");
//                 Console.WriteLine(ex.StackTrace);
//             }
//         }

        
//         /// Helper method to split CSV lines correctly, handling quoted fields
//         private static string[] SplitCsvLine(string line)
//         {
//             if (string.IsNullOrEmpty(line))
//             {
//                 return Array.Empty<string>();
//             }
            
//             var result = new List<string>();
//             bool inQuotes = false;
//             var currentValue = new StringBuilder();
            
//             for (int i = 0; i < line.Length; i++)
//             {
//                 char c = line[i];
                
//                 if (c == '"')
//                 {
                   
//                     if (inQuotes && i < line.Length - 1 && line[i + 1] == '"')
//                     {
//                         // Escaped quote (two double quotes)
//                         currentValue.Append('"');
//                         i++; // Skip the next quote
//                     }
//                     else
//                     {
//                         // Toggle quote mode
//                         inQuotes = !inQuotes;
//                     }
//                 }
//                 else if (c == ',' && !inQuotes)
//                 {
//                     // End of field
//                     result.Add(currentValue.ToString());
//                     currentValue.Clear();
//                 }
//                 else
//                 {
//                     // Regular character
//                     currentValue.Append(c);
//                 }
//             }
            
//             // Add the last field
//             result.Add(currentValue.ToString());
            
//             return result.ToArray();
//         }
        
       
//         /// Processes image paths from CSV to standardized web paths
        
//         private static string ProcessImagePath(string rawImagePath)
//         {
//             if (string.IsNullOrWhiteSpace(rawImagePath))
//                 return "/images/products/no-image.jpg";
            
//             // Handle array format like ['C:\\DatasetLicenta\\women_images\\FABRIC BELT BAG_img_0.jpg', 'C:\\DatasetLicenta\\women_images\\FABRIC BELT BAG_img_1.jpg']
//             if (rawImagePath.StartsWith("[") && rawImagePath.EndsWith("]"))
//             {
//                 // Extract the first image path from the array
//                 int firstQuoteStart = rawImagePath.IndexOf('\'') + 1;
//                 int firstQuoteEnd = rawImagePath.IndexOf('\'', firstQuoteStart);
                
//                 if (firstQuoteStart > 0 && firstQuoteEnd > firstQuoteStart)
//                 {
//                     string firstImagePath = rawImagePath.Substring(firstQuoteStart, firstQuoteEnd - firstQuoteStart);
                    
//                     // Extract the filename from the path
//                     int lastSlash = Math.Max(firstImagePath.LastIndexOf('\\'), firstImagePath.LastIndexOf('/'));
//                     if (lastSlash >= 0 && lastSlash < firstImagePath.Length - 1)
//                     {
//                         string fileName = firstImagePath.Substring(lastSlash + 1);
//                         return $"/images/products/{fileName}";
//                     }
//                 }
//             }
            
//             // Standard path processing
//             string cleanPath = rawImagePath.Replace("[", "").Replace("]", "").Replace("'", "").Replace("\"", "").Trim();
            
//             if (cleanPath.Contains("\\"))
//             {
//                 int lastBackslash = cleanPath.LastIndexOf("\\");
//                 string fileName = lastBackslash >= 0 ? cleanPath.Substring(lastBackslash + 1) : cleanPath;
//                 return $"/images/products/{fileName}";
//             }
            
//             if (cleanPath.StartsWith("/"))
//             {
//                 return cleanPath;
//             }
            
//             return $"/images/products/{cleanPath}";
//         }

//         /// Adds sample products directly to the database
//         /// Useful for testing when you don't have a CSV file
        
//         public static async Task AddSampleProductsDirectly(ApplicationDbContext context)
//         {
//             if (await context.Products.AnyAsync())
//             {
//                 Console.WriteLine("Products already exist, skipping direct sample products");
//                 return;
//             }
            
//             Console.WriteLine("Adding sample products directly to database...");
            
//             try
//             {
//                 var products = new List<Product>
//                 {
//                     new Product
//                     {
//                         ProductId = 1001,
//                         Name = "TEST PRODUCT - Leather Bag",
//                         Description = "A stylish leather bag",
//                         Price = 49.99m,
//                         Color = "Brown",
//                         Size = "One Size",
//                         ImageUrl = "/images/products/sample1.jpg",
//                         CategoryId = 1
//                     },
//                     new Product
//                     {
//                         ProductId = 1002,
//                         Name = "TEST PRODUCT - Blazer",
//                         Description = "An elegant blazer",
//                         Price = 89.99m,
//                         Color = "Black",
//                         Size = "M",
//                         ImageUrl = "/images/products/sample2.jpg",
//                         CategoryId = 2
//                     }
//                 };
                
//                 await context.Products.AddRangeAsync(products);
//                 await context.SaveChangesAsync();
                
//                 Console.WriteLine($"Successfully added {products.Count} sample products directly");
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine($"Error adding sample products: {ex.Message}");
//             }
//         }
        
       
//         /// Verifies the database state by checking if categories and products exist
        
//         public static async Task VerifyDatabaseState(ApplicationDbContext context)
//         {
//             int categoriesCount = await context.Categories.CountAsync();
//             int productsCount = await context.Products.CountAsync();
            
//             Console.WriteLine("=== Database State Verification ===");
//             Console.WriteLine($"Categories: {categoriesCount}");
//             Console.WriteLine($"Products: {productsCount}");
            
//             if (categoriesCount == 0)
//             {
//                 Console.WriteLine("WARNING: No categories found in database.");
//             }
            
//             if (productsCount == 0)
//             {
//                 Console.WriteLine("WARNING: No products found in database.");
//             }
            
//             if (categoriesCount > 0 && productsCount > 0)
//             {
//                 Console.WriteLine("Database state is valid.");
//             }
            
//             Console.WriteLine("================================");
//         }
//     }
// }