# Week 3 Changes - Cart, Order Processing and Payment Simulation

Implemented Week 3 roadmap requirements:

- Added session-based cart system.
  - Users can add menu items to cart.
  - Users can select customization options during item selection.
  - Users can update quantities.
  - Users can remove cart items.
  - Cart total dynamically includes customization price modifiers.

- Added order data model.
  - `Order`
  - `OrderItem`
  - `OrderItemCustomizationSelection`
  - `OrderStatus`
  - EF Core migration: `20260501120000_AddWeek3OrdersCart`

- Added payment simulation.
  - Payment page collects card holder, card number, expiry and CVV.
  - Basic validation is implemented.
  - Full order summary is displayed before payment.
  - No real payment provider is used.

- Added order completion flow.
  - Successful simulated payment creates an order in the database.
  - Selected menu items and customization choices are stored.
  - Order status is initialized as `Completed`.
  - User can view order success, order history and order details.

Main files added/updated:

- `Controllers/CartController.cs`
- `Models/Entities/Order.cs`
- `Models/Entities/OrderItem.cs`
- `Models/Entities/OrderItemCustomizationSelection.cs`
- `Models/Entities/OrderStatus.cs`
- `Models/ViewModels/CartViewModels.cs`
- `Views/Cart/*`
- `Views/Home/Index.cshtml`
- `Views/Shared/_Layout.cshtml`
- `Data/ApplicationDbContext.cs`
- `Program.cs`
- `wwwroot/css/site.css`
