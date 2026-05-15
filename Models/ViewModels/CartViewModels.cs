using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CaterFlow.Models.ViewModels
{
    public class AddToCartViewModel
    {
        public int MenuItemId { get; set; }
        public int Quantity { get; set; } = 1;
        public List<int> SelectedOptionIds { get; set; } = new();
    }

    public class CartItemViewModel
    {
        public Guid CartItemId { get; set; }
        public int MenuItemId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string CatererName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal CustomizationTotal { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal => (UnitPrice + CustomizationTotal) * Quantity;
        public List<CartCustomizationViewModel> SelectedCustomizations { get; set; } = new();
    }

    public class CartCustomizationViewModel
    {
        public int OptionId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string OptionName { get; set; } = string.Empty;
        public decimal PriceModifier { get; set; }
        public bool IsRemovableIngredient { get; set; }
    }

    public class CartPageViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();
        public decimal TotalPrice => Items.Sum(x => x.LineTotal);
    }

    public class PaymentViewModel : IValidatableObject
    {
        public CartPageViewModel Cart { get; set; } = new();

        [Required]
        [StringLength(120)]
        [Display(Name = "Card Holder Name")]
        public string CardHolderName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Card Number")]
        public string CardNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Expiration Date")]
        public string ExpirationDate { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV must be 3 or 4 digits.")]
        public string Cvv { get; set; } = string.Empty;

        public string GetCardDigits()
        {
            return (CardNumber ?? string.Empty)
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty);
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var cardDigits = GetCardDigits();
            if (cardDigits.Length != 16 || cardDigits.Any(c => !char.IsDigit(c)))
            {
                yield return new ValidationResult(
                    "Card number must contain 16 digits.",
                    new[] { nameof(CardNumber) });
            }

            var expiration = (ExpirationDate ?? string.Empty).Trim();
            if (!Regex.IsMatch(expiration, @"^(0[1-9]|1[0-2])\/\d{2}$"))
            {
                yield return new ValidationResult(
                    "Expiration date must be MM/YY.",
                    new[] { nameof(ExpirationDate) });
                yield break;
            }

            var month = int.Parse(expiration[..2]);
            var year = 2000 + int.Parse(expiration[^2..]);
            var expiresAt = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59, DateTimeKind.Utc);

            if (expiresAt < DateTime.UtcNow)
            {
                yield return new ValidationResult(
                    "Expiration date must be in the future.",
                    new[] { nameof(ExpirationDate) });
            }
        }
    }

    public class OrderHistoryListViewModel
    {
        public List<OrderHistoryItemViewModel> Orders { get; set; } = new();
        public string? StatusFilter { get; set; }
        public string? SearchTerm { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
    }

    public class OrderHistoryItemViewModel
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public int ItemCount { get; set; }
    }

    public class OrderDetailsViewModel
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string CardLastFourDigits { get; set; } = string.Empty;
        public bool HasBeenRated { get; set; }
        public List<CartItemViewModel> Items { get; set; } = new();
    }

    public class OrderReceiptViewModel : OrderDetailsViewModel
    {
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string? CustomerAddress { get; set; }
        public string ReceiptNumber => $"CF-{Id:000000}";
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class OrderAgreementViewModel : OrderReceiptViewModel
    {
        public string CatererBusinessName { get; set; } = string.Empty;
        public string CatererEmail { get; set; } = string.Empty;
        public string? CatererAddress { get; set; }
        public string AgreementNumber => $"AGR-{Id:000000}";
        public string DeliveryTerms => "Delivery within the estimated time frame. Freshly prepared upon order confirmation.";
        public string CancellationPolicy => "Orders may be cancelled within 15 minutes of placement. After preparation begins, cancellations are not accepted.";
        public string LiabilityClause => "WeHungry acts as an intermediary platform. Food quality and safety are the responsibility of the caterer.";
    }
}
