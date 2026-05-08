# Week 4 — Integration Features & System Intelligence

## Summary

This week implements five major features: Google Maps integration for location-based filtering,
a dual rating system (menu items + caterers), email notifications, a system logging mechanism,
and an admin log viewer interface.

---

## 1. Google Maps API Integration

- Users and caterers can set their location via **Account → Update Location**
- Browser geolocation auto-detection supported
- Location stored in `AppUser` (Latitude, Longitude, Address) and `CatererProfile`
- **Home page** filters restaurants by proximity using the Haversine distance formula
- Distance displayed on each menu card as a badge (e.g. "📍 3.2 km")
- Optional Google Maps embed shows caterer markers on a map
- Configurable max distance filter (5–500 km)

### Configuration

Set your Google Maps API key in `appsettings.json`:

```json
"GoogleMaps": {
  "ApiKey": "YOUR_API_KEY_HERE"
}
```

The system works without an API key (map just won't render), but filtering still works.

---

## 2. Rating System (Dual Structure)

- **Rating entity** (`Models/Entities/Rating.cs`) supports both menu item and caterer ratings
- Ratings are linked to completed orders — users can only rate after payment
- Each rating includes: Score (1–5), optional Comment, timestamp
- Average ratings are dynamically calculated and stored on `MenuItem.AverageRating` and `CatererProfile.AverageRating`
- "⭐ Rate" button appears on order history and order detail pages
- Already-rated items show existing scores (no duplicate ratings)
- `RatingController` handles the form submission

### New files:

- `Models/Entities/Rating.cs`
- `Models/ViewModels/RatingViewModels.cs`
- `Controllers/RatingController.cs`
- `Views/Rating/Rate.cshtml`

---

## 3. Email System

- `EmailService` sends HTML emails via SMTP
- **Order confirmation** sent to user after checkout
- **Order notification** sent to caterer(s) after checkout
- Emails include full order summary (items, quantities, prices)
- Graceful degradation: if SMTP not configured, emails are skipped with a warning log

### Configuration

Set SMTP credentials in `appsettings.json`:

```json
"Email": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "SenderEmail": "your-email@gmail.com",
  "SenderPassword": "your-app-password",
  "SenderName": "CaterFlow"
}
```

---

## 4. Logging System

- `SystemLog` entity records all system events with: EventType, Action, Details, UserId, UserEmail, IP, Level, Timestamp
- `LoggingService` provides typed logging methods:
  - `LogAuthAsync` — login, register, logout, failed login
  - `LogOrderAsync` — order creation
  - `LogPaymentAsync` — payment processing
  - `LogRatingAsync` — rating submissions
  - `LogErrorAsync` — errors
- Indexed on `CreatedAt` and `EventType` for efficient querying
- Integrated into `AccountController`, `CartController`, and `RatingController`

### New files:

- `Models/Entities/SystemLog.cs`
- `Services/LoggingService.cs`

---

## 5. Admin Logging Interface

- Admins can access logs at **Admin → System Logs**
- Supports filtering by: Event Type, Level, Search term
- Pagination (25 logs per page)
- Color-coded log levels (Info=blue, Warning=yellow, Error=red)
- Dashboard updated with Order, Rating, and Log counts

### New files:

- `Views/Admin/Logs.cshtml`
- `Models/ViewModels/AdminLogViewModels.cs`

---

## Migration

Run the following to apply the database changes:

```bash
dotnet ef database update
```

Migration name: `Week4RatingAndLogging`

---

## Updated Files

| File | Changes |
|------|---------|
| `Data/ApplicationDbContext.cs` | Added `Ratings`, `SystemLogs` DbSets and relationships |
| `Controllers/AccountController.cs` | Added logging + location update endpoint |
| `Controllers/CartController.cs` | Added logging + email after checkout |
| `Controllers/HomeController.cs` | Location-based filtering with Haversine formula |
| `Controllers/AdminController.cs` | Log viewer with filtering and pagination |
| `Program.cs` | Registered `LoggingService` and `EmailService` |
| `appsettings.json` | Added Email and GoogleMaps configuration |
| `Views/Shared/_Layout.cshtml` | Added nav links for Location and Logs |
| `Views/Home/Index.cshtml` | Location filter UI, distance badges, map embed |
| `Views/Cart/Details.cshtml` | Rate Order button |
| `Views/Cart/Orders.cshtml` | Rate button per order |
| `Views/Admin/Index.cshtml` | Additional stat cards |
| `wwwroot/css/site.css` | Week 4 styles |
