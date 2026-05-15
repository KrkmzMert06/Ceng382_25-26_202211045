using CaterFlow.Data;
using CaterFlow.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CaterFlow.Services
{
    public class DemoDataSeeder
    {
        private const string DemoPassword = "Demo123!";
        private readonly ApplicationDbContext _context;
        private readonly PasswordService _passwordService;

        public DemoDataSeeder(ApplicationDbContext context, PasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        public async Task SeedAsync()
        {
            await RemoveLegacyTekelAsync();
            await SeedSystemAccountsAsync();

            var restaurants = GetRestaurants();

            foreach (var restaurant in restaurants)
            {
                var existingUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.Email == restaurant.Email);
                if (existingUser != null)
                {
                    await SyncExistingRestaurantAsync(existingUser, restaurant);
                    continue;
                }

                var user = new AppUser
                {
                    FullName = restaurant.OwnerName,
                    Email = restaurant.Email,
                    PasswordHash = _passwordService.HashPassword(DemoPassword),
                    Role = UserRole.Caterer,
                    Address = restaurant.Address,
                    Latitude = restaurant.Latitude,
                    Longitude = restaurant.Longitude,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AppUsers.Add(user);
                await _context.SaveChangesAsync();

                var caterer = new CatererProfile
                {
                    AppUserId = user.Id,
                    BusinessName = restaurant.BusinessName,
                    Description = restaurant.Description,
                    Address = restaurant.Address,
                    Latitude = restaurant.Latitude,
                    Longitude = restaurant.Longitude,
                    AverageRating = restaurant.AverageRating
                };

                _context.CatererProfiles.Add(caterer);
                await _context.SaveChangesAsync();

                foreach (var menu in restaurant.MenuItems)
                {
                    _context.MenuItems.Add(BuildMenuItem(caterer.Id, menu));
                }

                await _context.SaveChangesAsync();
            }
        }

        private async Task SyncExistingRestaurantAsync(AppUser user, DemoRestaurant restaurant)
        {
            user.FullName = restaurant.OwnerName;
            user.Address = restaurant.Address;
            user.Latitude = restaurant.Latitude;
            user.Longitude = restaurant.Longitude;

            var caterer = await _context.CatererProfiles.FirstOrDefaultAsync(c => c.AppUserId == user.Id);
            if (caterer == null) return;

            caterer.BusinessName = restaurant.BusinessName;
            caterer.Description = restaurant.Description;
            caterer.Address = restaurant.Address;
            caterer.Latitude = restaurant.Latitude;
            caterer.Longitude = restaurant.Longitude;
            caterer.AverageRating = restaurant.AverageRating;

            var existingMenuItems = await _context.MenuItems
                .Where(m => m.CatererProfileId == caterer.Id)
                .ToListAsync();

            var oldChocolateCake = existingMenuItems.FirstOrDefault(m =>
                string.Equals(m.Name, "Chocolate Cake Slices", StringComparison.OrdinalIgnoreCase));
            var newVanillaCake = restaurant.MenuItems.FirstOrDefault(m =>
                string.Equals(m.Name, "Vanilla Cake Slices", StringComparison.OrdinalIgnoreCase));

            if (oldChocolateCake != null && newVanillaCake != null)
            {
                oldChocolateCake.Name = newVanillaCake.Name;
            }

            foreach (var menu in restaurant.MenuItems)
            {
                var existingMenu = existingMenuItems.FirstOrDefault(m =>
                    string.Equals(m.Name, menu.Name, StringComparison.OrdinalIgnoreCase));

                if (existingMenu == null)
                {
                    _context.MenuItems.Add(BuildMenuItem(caterer.Id, menu));
                    continue;
                }

                existingMenu.Description = menu.Description;
                existingMenu.Price = menu.Price;
                existingMenu.ImageUrl = menu.ImageUrl;
                existingMenu.AverageRating = menu.AverageRating;
                existingMenu.IsActive = true;
            }

            var desiredMenuNames = restaurant.MenuItems
                .Select(m => m.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var staleMenu in existingMenuItems.Where(m =>
                !desiredMenuNames.Contains(m.Name) &&
                (m.Name.Contains("Chocolate", StringComparison.OrdinalIgnoreCase) ||
                 m.ImageUrl.StartsWith("/uploads/menu-items/", StringComparison.OrdinalIgnoreCase))))
            {
                staleMenu.IsActive = false;
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedSystemAccountsAsync()
        {
            // Seed Admin account
            if (!await _context.AppUsers.AnyAsync(u => u.Email == "admin@caterflow.local"))
            {
                _context.AppUsers.Add(new AppUser
                {
                    FullName = "System Admin",
                    Email = "admin@caterflow.local",
                    PasswordHash = _passwordService.HashPassword(DemoPassword),
                    Role = UserRole.Admin,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            // Seed demo Customer account
            if (!await _context.AppUsers.AnyAsync(u => u.Email == "demo@caterflow.local"))
            {
                _context.AppUsers.Add(new AppUser
                {
                    FullName = "Demo Customer",
                    Email = "demo@caterflow.local",
                    PasswordHash = _passwordService.HashPassword(DemoPassword),
                    Role = UserRole.User,
                    Address = "Kadikoy, Istanbul",
                    Latitude = 40.9919,
                    Longitude = 29.0278,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }

        private async Task RemoveLegacyTekelAsync()
        {
            var tekelProfiles = await _context.CatererProfiles
                .Include(c => c.AppUser)
                .Where(c => c.BusinessName == "Tekel")
                .ToListAsync();

            foreach (var caterer in tekelProfiles)
            {
                var menuItems = await _context.MenuItems
                    .Include(m => m.CustomizationGroups)
                        .ThenInclude(g => g.Options)
                    .Where(m => m.CatererProfileId == caterer.Id)
                    .ToListAsync();

                foreach (var menuItem in menuItems)
                {
                    menuItem.IsActive = false;
                }

                caterer.BusinessName = "Tekel (Archived)";
                caterer.Description = "Archived demo restaurant hidden from the storefront.";
            }

            if (tekelProfiles.Any())
                await _context.SaveChangesAsync();
        }

        private static MenuItem BuildMenuItem(int catererProfileId, DemoMenuItem menu)
        {
            return new MenuItem
            {
                CatererProfileId = catererProfileId,
                Name = menu.Name,
                Description = menu.Description,
                Price = menu.Price,
                ImageUrl = menu.ImageUrl,
                AverageRating = menu.AverageRating,
                IsActive = true,
                CustomizationGroups = new List<MenuItemCustomizationGroup>
                {
                    new()
                    {
                        Name = "Porsiyon Seçimi",
                        IsRequired = true,
                        AllowsMultipleSelection = false,
                        DisplayOrder = 1,
                        Options = new List<MenuItemCustomizationOption>
                        {
                            new() { Name = "1 Porsiyon", PriceModifier = 0, IsDefaultSelected = true, DisplayOrder = 1 },
                            new() { Name = "1.5 Porsiyon", PriceModifier = menu.LargePortionPrice, DisplayOrder = 2 }
                        }
                    }
                }
            };
        }

        private static List<DemoRestaurant> GetRestaurants()
        {
            return new List<DemoRestaurant>
            {
                new("Mutfak No. 34", "demo-mutfak34@caterflow.local", "Elif Demir",
                    "Home-style Turkish meals for offices and daily lunch boxes.", "Kadikoy, Istanbul", 40.9919, 29.0278, 4.7,
                    new()
                    {
                        new("Classic Office Menu", "Lentil Soup • Chicken Sauté • Butter Rice • Seasonal Salad • Rice Pudding", 450, "/images/demo-menu/chicken-rice-bowl.png", 4.8, 45),
                        new("Traditional Kofte Set", "Tomato Soup • Grilled Meatballs • Bulgur Pilaf • Shepherd's Salad • Ayran", 520, "/images/demo-menu/lentil-soup-set.png", 4.6, 25),
                        new("Aegean Vegan Package", "Vegetable Soup • Olive Oil Artichoke • Bulgur • Hummus • Fruit Salad", 410, "/images/demo-menu/vegetarian-aegean-plate.png", 4.5, 40),
                        new("Premium Mixed Grill", "Yogurt Soup • Mixed Grill (Adana, Chicken) • Rice • Meze Plate • Baklava", 850, "/images/demo-menu/meatball-lunch-box.png", 4.7, 55)
                    }),

                new("Bosphorus Bites", "demo-bosphorus@caterflow.local", "Can Arslan",
                    "Modern sandwiches, wraps and meeting platters.", "Besiktas, Istanbul", 41.0438, 29.0094, 4.5,
                    new()
                    {
                        new("Executive Sandwich Box", "Smoked Turkey Sandwich • Potato Chips • Coleslaw • Brownie • Canned Drink", 320, "https://loremflickr.com/800/600/sandwich,food/all?lock=1", 4.4, 35),
                        new("Vegan Wrap Package", "Falafel Wrap • Hummus Dip • Carrot Sticks • Vegan Cookie • Cold Tea", 290, "https://loremflickr.com/800/600/falafel,wrap/all?lock=2", 4.6, 35),
                        new("Party Slider Platter (10 Pax)", "Assorted Mini Sliders • Onion Rings • French Fries • Dips • 1L Coke", 1450, "https://loremflickr.com/800/600/burger,slider/all?lock=3", 4.5, 90),
                        new("Healthy Meeting Lunch", "Chicken Caesar Salad • Whole Wheat Bread • Fresh Fruit Bowl • Detox Juice", 340, "https://loremflickr.com/800/600/salad,caesar/all?lock=4", 4.3, 40)
                    }),

                new("Anatolia Kitchen", "demo-anatolia@caterflow.local", "Zeynep Kaya",
                    "Traditional Anatolian catering with rich daily specials.", "Sisli, Istanbul", 41.0602, 28.9877, 4.8,
                    new()
                    {
                        new("Anatolian Feast Menu", "Ezogelin Soup • Slow-cooked Beef Stew • Rice Pilaf • Cacik • Pistachio Baklava", 680, "https://loremflickr.com/800/600/beef,stew/all?lock=5", 4.9, 65),
                        new("Home-Style Veggie Set", "Tarhana Soup • Stuffed Bell Peppers • Yogurt • Green Salad • Semolina Halva", 380, "https://loremflickr.com/800/600/stuffed,pepper,food/all?lock=6", 4.7, 40),
                        new("Classic Kebab Box", "Mercimek Soup • Urfa Kebab • Roasted Tomatoes • Onion Salad • Ayran", 550, "https://loremflickr.com/800/600/soup,bowl/all?lock=7", 4.6, 25),
                        new("Dessert Tasting Tray", "Walnut Baklava • Kadayif • Rice Pudding • Turkish Delight • Black Tea", 1850, "https://loremflickr.com/800/600/baklava,dessert/all?lock=8", 4.8, 75)
                    }),

                new("Green Bowl Co.", "demo-greenbowl@caterflow.local", "Mert Yilmaz",
                    "Healthy bowls, salads and vegan-friendly office meals.", "Levent, Istanbul", 41.0819, 29.0128, 4.6,
                    new()
                    {
                        new("Fit Office Package", "Pumpkin Soup • Grilled Chicken Breast • Quinoa Salad • Boiled Veggies • Green Tea", 420, "https://loremflickr.com/800/600/quinoa,bowl/all?lock=9", 4.7, 55),
                        new("Vegan Power Menu", "Broccoli Soup • Vegan Buddha Bowl • Sweet Potato Fries • Tahini Dip • Kombucha", 390, "https://loremflickr.com/800/600/buddha,bowl,food/all?lock=10", 4.6, 50),
                        new("Light Pescatarian Set", "Mushroom Soup • Tuna Salad • Boiled Egg • Whole Grain Crisps • Mineral Water", 350, "https://loremflickr.com/800/600/tuna,salad/all?lock=11", 4.4, 45),
                        new("Detox Juice Bundle", "Green Detox Juice • Carrot Ginger Juice • Beetroot Drink • Fruit Salad • Nuts", 480, "https://loremflickr.com/800/600/juice,bottle/all?lock=12", 4.5, 30)
                    }),

                new("Pide & Lahmacun House", "demo-pidehouse@caterflow.local", "Burak Aydin",
                    "Stone-oven pide, lahmacun and group-friendly Turkish classics.", "Uskudar, Istanbul", 41.0255, 29.0157, 4.4,
                    new()
                    {
                        new("Team Pide Menu (5 Pax)", "5 Mixed Pides • Large Shepherd's Salad • Roasted Peppers • Pickles • 1L Ayran", 1250, "https://loremflickr.com/800/600/pide,pizza/all?lock=13", 4.4, 35),
                        new("Lahmacun Party Box (10 Pax)", "20 Spicy Lahmacuns • Fresh Greens Plate • Tomatoes & Lemons • Ezme • 2L Cola", 1850, "https://loremflickr.com/800/600/lahmacun,food/all?lock=14", 4.5, 45),
                        new("Individual Pide Box", "Lentil Soup • Minced Beef Pide • Ezme Salad • Small Dessert • Ayran", 280, "https://loremflickr.com/800/600/turkish,pide/all?lock=15", 4.3, 35),
                        new("Wrap & Pide Combo", "Mini Cheese Pides • Chicken Wraps • Hummus • Fries • Assorted Drinks", 320, "https://loremflickr.com/800/600/ayran,drink/all?lock=16", 4.2, 20)
                    }),

                new("Bella Pasta Catering", "demo-bellapasta@caterflow.local", "Selin Rossi",
                    "Italian pasta trays, salads and office-friendly desserts.", "Maslak, Istanbul", 41.1128, 29.0194, 4.7,
                    new()
                    {
                        new("Italian Lunch Box", "Minestrone Soup • Penne Arrabbiata • Caprese Salad • Garlic Bread • Tiramisu", 460, "https://loremflickr.com/800/600/penne,pasta/all?lock=17", 4.6, 40),
                        new("Premium Pasta Set", "Tomato Basil Soup • Chicken Alfredo • Bruschetta • Caesar Salad • Panna Cotta", 490, "https://loremflickr.com/800/600/fettuccine,pasta/all?lock=18", 4.8, 55),
                        new("Catering Pasta Tray (10 Pax)", "Large Baked Ziti Tray • 10 Garlic Breads • Large Mixed Salad • Assorted Drinks", 2100, "https://loremflickr.com/800/600/caprese,salad/all?lock=19", 4.5, 35),
                        new("Dessert & Coffee Box", "Tiramisu Cups • Cannoli Pastries • Biscotti • Fresh Strawberries • Cold Brew", 280, "https://loremflickr.com/800/600/tiramisu,dessert/all?lock=20", 4.7, 30)
                    }),

                new("Sushi Box Istanbul", "demo-sushibox@caterflow.local", "Deniz Sato",
                    "Fresh sushi boxes and Asian catering sets.", "Atasehir, Istanbul", 40.9923, 29.1244, 4.3,
                    new()
                    {
                        new("Executive Sushi Set", "Miso Soup • California Roll (8pcs) • Edamame Bowl • Wakame Salad • Green Tea", 650, "https://loremflickr.com/800/600/sushi,roll/all?lock=21", 4.3, 60),
                        new("Nigiri Tasting Menu", "Tom Yum Soup • Assorted Nigiri (Salmon/Tuna) • Spring Rolls • Soy & Ginger • Mochi", 720, "https://loremflickr.com/800/600/sushi,salmon/all?lock=22", 4.4, 70),
                        new("Vegan Asian Box", "Clear Veggie Soup • Veggie Sushi Rolls • Gyoza (Vegetable) • Asian Slaw • Jasmine Tea", 480, "https://loremflickr.com/800/600/sushi,vegetarian/all?lock=23", 4.2, 50),
                        new("Hot Noodle Package", "Egg Drop Soup • Chicken Noodle Bowl • Steamed Dumplings • Kimchi • Soda", 520, "https://loremflickr.com/800/600/noodles,bowl/all?lock=24", 4.1, 45)
                    }),

                new("Sweet Break Bakery", "demo-sweetbreak@caterflow.local", "Ayse Kurt",
                    "Breakfast pastries, cakes and dessert catering.", "Bakirkoy, Istanbul", 40.9769, 28.8737, 4.9,
                    new()
                    {
                        new("Morning Meeting Box", "Assorted Croissants • Cheese Selection • Cherry Jam • Olives • Filter Coffee", 450, "https://loremflickr.com/800/600/pastry,food/all?lock=25", 4.9, 60),
                        new("Celebration Cake Set", "Large Vanilla Cake • Mini Cupcakes • Party Plates & Forks • 2L Lemonade", 850, "https://loremflickr.com/800/600/cake,vanilla/all?lock=26", 4.8, 40),
                        new("Coffee Break Platter", "Chocolate Brownies • Assorted Cookies • Fresh Fruits • Macarons • Tea Packets", 620, "https://loremflickr.com/800/600/cookie,platter/all?lock=27", 4.7, 50),
                        new("High Tea Package", "Mini Fruit Tarts • Cucumber Finger Sandwiches • Scones • Clotted Cream • Herbal Teas", 780, "https://loremflickr.com/800/600/tart,fruit/all?lock=28", 4.8, 45)
                    })
            };
        }

        private record DemoRestaurant(
            string BusinessName,
            string Email,
            string OwnerName,
            string Description,
            string Address,
            double Latitude,
            double Longitude,
            double AverageRating,
            List<DemoMenuItem> MenuItems);

        private record DemoMenuItem(
            string Name,
            string Description,
            decimal Price,
            string ImageUrl,
            double AverageRating,
            decimal LargePortionPrice);
    }
}
