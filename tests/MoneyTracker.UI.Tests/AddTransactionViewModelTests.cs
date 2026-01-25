using System.Linq;
using Xunit;
using MoneyTracker.UI.ViewModels;
using MoneyTracker.Models;

namespace MoneyTracker.UI.Tests;

public class AddTransactionViewModelTests
{
    [Fact]
    public void UpdateFilteredCategories_FiltersBySelectedType()
    {
        var vm = new AddTransactionViewModel();
        vm.Categories = new System.Collections.Generic.List<Category>
        {
            new Category { Id = 1, Name = "Salary", Type = MoneyTracker.Models.CategoryType.Income },
            new Category { Id = 2, Name = "Food", Type = MoneyTracker.Models.CategoryType.Expense },
            new Category { Id = 3, Name = "Investment", Type = MoneyTracker.Models.CategoryType.Income }
        };

        // select Expense
        vm.SelectTypeCommand.Execute("Expense");

        Assert.All(vm.FilteredCategories, c => Assert.Equal(MoneyTracker.Models.CategoryType.Expense, c.Type));
        Assert.Single(vm.FilteredCategories);

        // select Income
        vm.SelectTypeCommand.Execute("Income");
        Assert.All(vm.FilteredCategories, c => Assert.Equal(MoneyTracker.Models.CategoryType.Income, c.Type));
        Assert.Equal(2, vm.FilteredCategories.Count);
    }
}
