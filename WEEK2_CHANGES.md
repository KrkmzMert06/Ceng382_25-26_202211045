# CaterFlow Week 2 Completion Notes

This version extends the original Week 1/2 project to satisfy the Week 2 roadmap requirements.

## Added

- Caretaker menu CRUD
  - Create menu item
  - Edit menu item
  - Delete menu item
  - Active/passive status

- Mandatory menu image handling
  - `IFormFile` upload from the UI
  - Stored under `wwwroot/uploads/menu-items`
  - Image URL saved in database
  - Images displayed in caretaker and user menu screens

- Dynamic customization system
  - `MenuItemCustomizationGroup`
  - `MenuItemCustomizationOption`
  - Removable ingredients are stored as boolean-style removable options
  - Optional additions and grouped options are stored dynamically in the database
  - Optional price modifiers are supported per option

- User menu view improvements
  - Image display
  - Caterer name display
  - Menu details display
  - Customization groups/options display

## How to enter customizations

In the Create/Edit menu screen:

### Removable Ingredients
Enter one ingredient per line:

```text
Tomato
Onion
Pickles
```

### Customization Groups
Use this format:

```text
Group: Sauces
Ketchup|0
Garlic Sauce|5

Group: Sides
Fries|20
Salad|15
```

The number after `|` is the optional price modifier.

## Database

A migration was added:

```text
20260424102000_AddWeek2MenuCustomization
```

Run the usual EF Core database update command in your environment after opening the project.
