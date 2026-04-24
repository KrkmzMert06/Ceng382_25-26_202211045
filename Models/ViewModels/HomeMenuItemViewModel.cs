namespace CaterFlow.Models.ViewModels
{
    public class HomeMenuItemViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string CatererName { get; set; } = string.Empty;
        public List<HomeCustomizationGroupViewModel> CustomizationGroups { get; set; } = new();
    }

    public class HomeCustomizationGroupViewModel
    {
        public string Name { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public bool AllowsMultipleSelection { get; set; }
        public List<HomeCustomizationOptionViewModel> Options { get; set; } = new();
    }

    public class HomeCustomizationOptionViewModel
    {
        public string Name { get; set; } = string.Empty;
        public decimal PriceModifier { get; set; }
        public bool IsRemovableIngredient { get; set; }
    }
}
