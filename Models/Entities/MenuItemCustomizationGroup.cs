using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaterFlow.Models.Entities
{
    public class MenuItemCustomizationGroup
    {
        public int Id { get; set; }

        [Required]
        public int MenuItemId { get; set; }

        [ForeignKey(nameof(MenuItemId))]
        public MenuItem MenuItem { get; set; } = null!;

        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        public bool IsRequired { get; set; }

        public bool AllowsMultipleSelection { get; set; } = true;

        public int DisplayOrder { get; set; }

        public ICollection<MenuItemCustomizationOption> Options { get; set; } = new List<MenuItemCustomizationOption>();
    }
}
