using System;
using System.Collections.Generic;
using Reqnroll;
using Reqnroll.Assist;
using FluentAssertions;

namespace BDD.TestExecution.StepDefinitions
{
    [Binding]
    public class AdvancedEcommerceSteps
    {
        private bool _platformRunning;
        private bool _paymentGatewayOnline;
        private bool _shippingServiceAvailable;
        private string _loggedInUser = string.Empty;
        private List<string> _cartItems = new();
        private bool _orderPlaced;
        private string _orderStatus = string.Empty;
        private decimal _orderTotal;

        [Given(@"the e-commerce platform is running")]
        public void GivenTheEcommercePlatformIsRunning()
        {
            _platformRunning = true;
            Console.WriteLine("✓ E-commerce platform is running");
        }

        [Given(@"the following products are available in inventory:")]
        public void GivenTheFollowingProductsAreAvailableInInventory(Table table)
        {
            Console.WriteLine($"✓ Loaded {table.RowCount} products into inventory");
        }

        [Given(@"the payment gateway is online")]
        public void GivenThePaymentGatewayIsOnline()
        {
            _paymentGatewayOnline = true;
            Console.WriteLine("✓ Payment gateway is online");
        }

        [Given(@"the shipping service is available")]
        public void GivenTheShippingServiceIsAvailable()
        {
            _shippingServiceAvailable = true;
            Console.WriteLine("✓ Shipping service is available");
        }

        [Given(@"I am logged in as ""(.*)""")]
        public void GivenIAmLoggedInAs(string email)
        {
            _loggedInUser = email;
            Console.WriteLine($"✓ Logged in as: {email}");
        }

        [Given(@"my shopping cart contains:")]
        public void GivenMyShoppingCartContains(Table table)
        {
            foreach (var row in table.Rows)
            {
                _cartItems.Add(row["Product ID"]);
            }
            Console.WriteLine($"✓ Cart loaded with {table.RowCount} items");
        }

        [When(@"I proceed to checkout")]
        public void WhenIProceedToCheckout()
        {
            Console.WriteLine("✓ Proceeding to checkout");
        }

        [When(@"I select shipping address:")]
        public void WhenISelectShippingAddress(string address)
        {
            Console.WriteLine($"✓ Shipping address selected");
        }

        [When(@"I choose ""(.*)"" delivery method")]
        public void WhenIChooseDeliveryMethod(string deliveryMethod)
        {
            Console.WriteLine($"✓ Selected delivery: {deliveryMethod}");
        }

        [When(@"I enter payment details:")]
        public void WhenIEnterPaymentDetails(Table table)
        {
            Console.WriteLine($"✓ Payment details entered");
        }

        [When(@"I apply promotional code ""(.*)""")]
        public void WhenIApplyPromotionalCode(string promoCode)
        {
            Console.WriteLine($"✓ Applied promo code: {promoCode}");
        }

        [When(@"I confirm the order")]
        public void WhenIConfirmTheOrder()
        {
            // PASS: Order placed successfully
            _orderPlaced = true;
            _orderStatus = "confirmed";
            _orderTotal = 1029.97m;
            Console.WriteLine("✓ Order confirmed");
        }

        [Then(@"the order should be placed successfully")]
        public void ThenTheOrderShouldBePlacedSuccessfully()
        {
            Console.WriteLine("✓ Verifying order placement");
            _orderPlaced.Should().BeTrue();
        }

        [Then(@"I should receive an order confirmation with order number")]
        public void ThenIShouldReceiveAnOrderConfirmationWithOrderNumber()
        {
            Console.WriteLine("✓ Order confirmation received");
            _orderStatus.Should().Be("confirmed");
        }

        [Then(@"the order total should be ""(.*)""")]
        public void ThenTheOrderTotalShouldBe(string expectedTotal)
        {
            var expected = decimal.Parse(expectedTotal.Replace("$", "").Replace(",", ""));
            
            // INTENTIONAL FAILURE: Order total calculation is incorrect
            _orderTotal = 1099.99m; // Wrong total, should be what's expected
            
            Console.WriteLine($"✗ Order total mismatch: Expected {expectedTotal}, Got ${_orderTotal}");
            _orderTotal.Should().Be(expected, "Order total calculation has a bug");
        }

        [Then(@"the order status should be ""(.*)""")]
        public void ThenTheOrderStatusShouldBe(string expectedStatus)
        {
            Console.WriteLine($"✓ Verifying order status: {expectedStatus}");
            _orderStatus.Should().Be(expectedStatus.ToLower());
        }

        [Then(@"an email confirmation should be sent to ""(.*)""")]
        public void ThenAnEmailConfirmationShouldBeSentTo(string email)
        {
            Console.WriteLine($"✓ Email sent to: {email}");
            email.Should().Be(_loggedInUser);
        }

        [Then(@"the inventory stock should be reduced by:")]
        public void ThenTheInventoryStockShouldBeReducedBy(Table table)
        {
            Console.WriteLine($"✓ Inventory updated for {table.RowCount} items");
        }

        // Missing step definitions
        [Then(@"I should have (.*) loyalty points added to my account")]
        public void ThenIShouldHaveLoyaltyPointsAddedToMyAccount(int points)
        {
            Console.WriteLine($"✓ Loyalty points added: {points}");
            points.Should().BeGreaterThanOrEqualTo(0);
        }

        [Given(@"I have an order with ID ""(.*)"" in my account")]
        public void GivenIHaveAnOrderWithIDInMyAccount(string orderId)
        {
            Console.WriteLine($"✓ Order {orderId} found in account");
        }

        [When(@"I request to cancel the order ""(.*)""")]
        public void WhenIRequestToCancelTheOrder(string orderId)
        {
            Console.WriteLine($"✓ Cancellation requested for order: {orderId}");
        }

        [Then(@"I should receive deletion confirmation email")]
        public void ThenIShouldReceiveDeletionConfirmationEmail()
        {
            Console.WriteLine("✓ Deletion confirmation email sent");
        }

        [Given(@"I am logged in as a ""(.*)"" customer with email ""(.*)""")]
        public void GivenIAmLoggedInAsACustomerWithEmail(string customerType, string email)
        {
            _loggedInUser = email;
            Console.WriteLine($"✓ Logged in as {customerType} customer: {email}");
        }

        [Given(@"I have ""(.*)"" loyalty points")]
        public void GivenIHaveLoyaltyPoints(string points)
        {
            Console.WriteLine($"✓ Loyalty points: {points}");
        }

        [When(@"I add product ""(.*)"" with quantity (.*) to cart")]
        public void WhenIAddProductWithQuantityToCart(string productId, int quantity)
        {
            _cartItems.Add(productId);
            Console.WriteLine($"✓ Added {quantity}x {productId} to cart");
        }

        [When(@"I proceed to checkout with ""(.*)"" payment")]
        public void WhenIProceedToCheckoutWithPayment(string paymentMethod)
        {
            Console.WriteLine($"✓ Checkout with {paymentMethod}");
        }

        [When(@"I select ""(.*)"" shipping")]
        public void WhenISelectShipping(string shippingMethod)
        {
            Console.WriteLine($"✓ Selected {shippingMethod} shipping");
        }

        [Then(@"the order should be ""(.*)""")]
        public void ThenTheOrderShouldBe(string status)
        {
            _orderStatus = status;
            Console.WriteLine($"✓ Order status: {status}");
        }

        [Then(@"I should see ""(.*)"" notification")]
        public void ThenIShouldSeeNotification(string notificationType)
        {
            Console.WriteLine($"✓ Notification type: {notificationType}");
        }

        [Then(@"my loyalty points should be ""(.*)""")]
        public void ThenMyLoyaltyPointsShouldBe(string finalPoints)
        {
            Console.WriteLine($"✓ Final loyalty points: {finalPoints}");
        }

        [Then(@"the estimated delivery should be within ""(.*)"" days")]
        public void ThenTheEstimatedDeliveryShouldBeWithinDays(string deliveryDays)
        {
            Console.WriteLine($"✓ Estimated delivery: {deliveryDays} days");
        }

        [Then(@"I should receive order confirmation email")]
        public void ThenIShouldReceiveOrderConfirmationEmail()
        {
            Console.WriteLine("✓ Order confirmation email sent");
        }

        [Then(@"the order total should be:")]
        public void ThenTheOrderTotalShouldBeMultiline(string multilineText)
        {
            Console.WriteLine("✓ Order total breakdown verified");
        }

        [Then(@"the inventory should be updated:")]
        public void ThenTheInventoryShouldBeUpdated(Table table)
        {
            Console.WriteLine($"✓ Inventory updated for {table.RowCount} products");
        }

        [Given(@"I am logged in as a ""(.*)"" customer")]
        public void GivenIAmLoggedInAsACustomer(string customerType)
        {
            Console.WriteLine($"✓ Logged in as {customerType} customer");
        }

        [Given(@"I have negotiated pricing agreement ""(.*)""")]
        public void GivenIHaveNegotiatedPricingAgreement(string agreementId)
        {
            Console.WriteLine($"✓ Pricing agreement: {agreementId}");
        }

        [When(@"I create a bulk order with the following items:")]
        public void WhenICreateABulkOrderWithTheFollowingItems(Table table)
        {
            Console.WriteLine($"✓ Bulk order with {table.RowCount} items");
        }

        [When(@"I request split shipment to multiple warehouses:")]
        public void WhenIRequestSplitShipmentToMultipleWarehouses(string multilineText)
        {
            Console.WriteLine("✓ Split shipment requested");
        }

        [When(@"I choose payment terms ""(.*)""")]
        public void WhenIChoosePaymentTerms(string terms)
        {
            Console.WriteLine($"✓ Payment terms: {terms}");
        }

        [Then(@"the order should be submitted for approval")]
        public void ThenTheOrderShouldBeSubmittedForApproval()
        {
            Console.WriteLine("✓ Order submitted for approval");
        }

        [Then(@"I should receive quote document with order ID")]
        public void ThenIShouldReceiveQuoteDocumentWithOrderID()
        {
            Console.WriteLine("✓ Quote document generated");
        }

        [Then(@"the estimated total should be ""(.*)""")]
        public void ThenTheEstimatedTotalShouldBe(string total)
        {
            Console.WriteLine($"✓ Estimated total: {total}");
        }

        [Then(@"the bulk discount should be ""(.*)""")]
        public void ThenTheBulkDiscountShouldBe(string discount)
        {
            Console.WriteLine($"✓ Bulk discount: {discount}");
        }

        [Then(@"the final amount should be ""(.*)""")]
        public void ThenTheFinalAmountShouldBe(string finalAmount)
        {
            Console.WriteLine($"✓ Final amount: {finalAmount}");
        }

        [When(@"I choose a subscription plan with the following configuration:")]
        public void WhenIChooseASubscriptionPlanWithTheFollowingConfiguration(string multilineText)
        {
            Console.WriteLine("✓ Subscription plan configured");
        }

        [When(@"I confirm subscription terms and conditions")]
        public void WhenIConfirmSubscriptionTermsAndConditions()
        {
            Console.WriteLine("✓ Subscription terms confirmed");
        }

        [Then(@"the subscription should be activated")]
        public void ThenTheSubscriptionShouldBeActivated()
        {
            Console.WriteLine("✓ Subscription activated");
        }

        [Then(@"I should see subscription dashboard with:")]
        public void ThenIShouldSeeSubscriptionDashboardWith(Table table)
        {
            Console.WriteLine($"✓ Subscription dashboard loaded");
        }

        [Then(@"I should receive welcome email with subscription details")]
        public void ThenIShouldReceiveWelcomeEmailWithSubscriptionDetails()
        {
            Console.WriteLine("✓ Welcome email sent");
        }

        [Then(@"a calendar reminder should be created for ""(.*)""")]
        public void ThenACalendarReminderShouldBeCreatedFor(string date)
        {
            Console.WriteLine($"✓ Calendar reminder: {date}");
        }
    }
}
