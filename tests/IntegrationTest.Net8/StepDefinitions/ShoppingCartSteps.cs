using System;
using System.Linq;
using Reqnroll;
using Reqnroll.Assist;
using FluentAssertions;

namespace BDD.TestExecution.StepDefinitions
{
    [Binding]
    public class ShoppingCartSteps
    {
        private bool _isLoggedIn;
        private int _cartItemCount;
        private decimal _cartTotal;
        private string _discountMessage = string.Empty;

        [Given(@"I am logged in as a customer")]
        public void GivenIAmLoggedInAsACustomer()
        {
            _isLoggedIn = true;
            Console.WriteLine("✓ Customer logged in");
        }

        [Given(@"my cart is empty")]
        public void GivenMyCartIsEmpty()
        {
            _cartItemCount = 0;
            _cartTotal = 0;
            Console.WriteLine("✓ Cart is empty");
        }

        [When(@"I navigate to product ""(.*)""")]
        public void WhenINavigateToProduct(string productName)
        {
            Console.WriteLine($"✓ Navigated to product: {productName}");
        }

        [When(@"I click ""(.*)""")]
        public void WhenIClick(string buttonName)
        {
            if (buttonName == "Add to Cart")
            {
                _cartItemCount++;
                _cartTotal = 999.99m; // PASS: Correct total
            }
            Console.WriteLine($"✓ Clicked: {buttonName}");
        }

        [Then(@"the cart should contain (.*) item")]
        public void ThenTheCartShouldContainItem(int expectedCount)
        {
            Console.WriteLine($"✓ Verifying cart has {expectedCount} item(s)");
            _cartItemCount.Should().Be(expectedCount);
        }

        [Then(@"the cart total should be ""(.*)""")]
        public void ThenTheCartTotalShouldBe(string expectedTotal)
        {
            var expected = decimal.Parse(expectedTotal.Replace("$", ""));
            Console.WriteLine($"✓ Verifying cart total: {expectedTotal}");
            _cartTotal.Should().Be(expected);
        }

        [Given(@"I have the following items in my cart:")]
        public void GivenIHaveTheFollowingItemsInMyCart(Table table)
        {
            _cartItemCount = table.RowCount;
            _cartTotal = 999.99m; // FAIL: Wrong calculation (should be 999.99 + 2*25 = 1049.99)
            Console.WriteLine($"✓ Cart loaded with {table.RowCount} different products");
        }

        [When(@"I remove ""(.*)"" from the cart")]
        public void WhenIRemoveFromTheCart(string productName)
        {
            _cartItemCount--;
            _cartTotal = 998.99m; // FAIL: Should be 999.99
            Console.WriteLine($"✓ Removed {productName} from cart");
        }

        [Given(@"I have items worth ""(.*)"" in my cart")]
        public void GivenIHaveItemsWorthInMyCart(string amount)
        {
            _cartTotal = decimal.Parse(amount.Replace("$", ""));
            Console.WriteLine($"✓ Cart value set to {amount}");
        }

        [When(@"I apply discount code ""(.*)""")]
        public void WhenIApplyDiscountCode(string discountCode)
        {
            if (discountCode == "SAVE20")
            {
                _cartTotal *= 0.8m; // Apply 20% discount
                _discountMessage = "Discount applied successfully";
            }
            Console.WriteLine($"✓ Applied discount code: {discountCode}");
        }

        [Then(@"I should see a message ""(.*)""")]
        public void ThenIShouldSeeAMessage(string expectedMessage)
        {
            Console.WriteLine($"✓ Verifying message: {expectedMessage}");
            _discountMessage.Should().Be(expectedMessage);
        }
    }
}
