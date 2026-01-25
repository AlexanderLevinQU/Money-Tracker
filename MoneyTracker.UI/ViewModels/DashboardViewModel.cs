using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoneyTracker.Models;
using MoneyTracker.UI.Services;
using MoneyTracker.UI.Services.Interfaces;
using MoneyTracker.UI.Models;
using MoneyTracker.UI.Enums;
using System.Collections.ObjectModel;

namespace MoneyTracker.UI.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IApiService _apiService;

    // Backing storage for all loaded transactions (used for paging)
    private List<TransactionDto> _allTransactions = new();

    // Public collection exposed to the UI (paged)
    private readonly ObservableCollection<TransactionDto> _transactions = new();
    public ObservableCollection<TransactionDto> Transactions => _transactions;

    // Options for the filter picker (bound from the view)
    public List<string> FilterOptions { get; } = new List<string> { "All", "Expenses", "Income", "Category" };

    private int _pageSize = 3;
    private int _currentPage = 0;
    private bool _hasMore;

    public bool HasMore
    {
        get => _hasMore;
        private set => SetProperty(ref _hasMore, value);
    }

    [ObservableProperty]
    private int selectedProfileId;

    [ObservableProperty]
    private decimal totalIncome;

    [ObservableProperty]
    private decimal totalExpenses;

    [ObservableProperty]
    private decimal netBalance;

    [ObservableProperty]
    private DateTime selectedDate = DateTime.Now;

    [ObservableProperty]
    private PeriodType timePeriod = PeriodType.Month;

    [ObservableProperty]
    private DateTime customStartDate = DateTime.Now.Date.AddMonths(-1);

    [ObservableProperty]
    private DateTime customEndDate = DateTime.Now.Date;

    [ObservableProperty]
    private int fiscalYearStartMonth = 4; // April by default; configurable

    [ObservableProperty]
    private TransactionFilter filterType = TransactionFilter.All;

    [ObservableProperty]
    private string filterCategory = string.Empty;

    [ObservableProperty]
    private string selectedFilter = "All";

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private int transactionsCount;

    [ObservableProperty]
    private List<CategorySummary> expenseSummary = new();

    [ObservableProperty]
    private List<CategorySummary> incomeSummary = new();

    [ObservableProperty]
    private List<string> availableCategories = new();

    public DashboardViewModel() : this(new ApiService()) { }

    public DashboardViewModel(IApiService apiService)
    {
        _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        // Listen for transaction saves and refresh dashboard/categories when they occur
        try
        {
            MessagingCenter.Subscribe<object, int>(this, "TransactionSaved", async (src, profileId) =>
            {
                if (profileId == SelectedProfileId)
                {
                    Logger.Log($"MessagingCenter: TransactionSaved -> LoadDashboard for profileId={profileId}");
                    await LoadDashboardCommand.ExecuteAsync(profileId);
                }
            });
        }
        catch { }
    }

    // Exposed boolean to drive Category picker visibility from XAML
    public bool IsCategoryFilterVisible => FilterType == TransactionFilter.Category;

    // Called by generated property change hook when FilterType changes
    partial void OnFilterTypeChanged(TransactionFilter value)
    {
        OnPropertyChanged(nameof(IsCategoryFilterVisible));
    }

    // Called when the FilterCategory property changes (bound from the UI)
    partial void OnFilterCategoryChanged(string value)
    {
        try
        {
            // trigger reload with the new category (async, fire-and-forget)
            Logger.Log($"OnFilterCategoryChanged: value='{value}' -> LoadDashboard for profileId={SelectedProfileId}");
            _ = LoadDashboardCommand.ExecuteAsync(SelectedProfileId);
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "OnFilterCategoryChanged");
        }
    }

    [RelayCommand]
    public async Task LoadDashboard(int profileId)
    {
        // Prevent concurrent loads which can cause UI churn and duplicate work
        if (IsLoading) 
        {
            Logger.Log($"LoadDashboard: skip concurrent load for profileId={profileId}");
            return;
        }
        SelectedProfileId = profileId;
        IsLoading = true;
        Logger.Log($"LoadDashboard: start for profileId={profileId}");
        try
        {
            var allTransactions = await _apiService.GetTransactionsAsync();
            _allTransactions = allTransactions
                .Where(t => t.ProfileId == profileId && IsInSelectedPeriod(t.Date) && MatchesFilter(t))
                .OrderByDescending(t => t.Date)
                .ToList();

            // reset paging and load first page
            TransactionsCount = _allTransactions.Count;
            Logger.Log($"LoadDashboard: transactions loaded totalCount={_allTransactions.Count}");
            _currentPage = 0;
            Transactions.Clear();
            await LoadPageAsync(reset: true);
            CalculateSummaries();

            // Refresh canonical category list from the API (per-profile) so the category picker
            // shows all categories for the selected profile regardless of current period/filter.
            try
            {
                var cats = await _apiService.GetCategoriesAsync();
                AvailableCategories = cats
                    .Select(c => c.Name)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s)
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Error loading categories");
            }
            Logger.Log($"LoadDashboard: totals computed Income={TotalIncome} Expenses={TotalExpenses} Net={NetBalance}");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Error loading dashboard");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RefreshData()
    {
        Logger.Log($"Caller: RefreshData -> LoadDashboard for profileId={SelectedProfileId}");
        await LoadDashboardCommand.ExecuteAsync(SelectedProfileId);
    }

    [RelayCommand]
    public async Task ChangePeriod(string period)
    {
        if (!Enum.TryParse<PeriodType>(period, ignoreCase: true, out var parsed))
        {
            parsed = PeriodType.Month;
        }
        TimePeriod = parsed;
        // Reload the dashboard with the new period
        Logger.Log($"Caller: ChangePeriod(string) parsed={parsed} -> LoadDashboard for profileId={SelectedProfileId}");
        await LoadDashboardCommand.ExecuteAsync(SelectedProfileId);
    }

    [RelayCommand]
    public async Task ChangeFilter(string filter)
    {
        if (!Enum.TryParse<TransactionFilter>(filter, ignoreCase: true, out var parsed))
        {
            parsed = TransactionFilter.All;
        }
        FilterType = parsed;
        Logger.Log($"Caller: ChangeFilter(string) parsed={parsed} -> LoadDashboard for profileId={SelectedProfileId}");
        await LoadDashboardCommand.ExecuteAsync(SelectedProfileId);
    }

    [RelayCommand]
    public async Task SetFilterCategory(string category)
    {
        // Set the property; the generated property-change hook will trigger the actual reload.
        var newCat = category ?? string.Empty;
        if (string.Equals(FilterCategory, newCat, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Log($"SetFilterCategory: category unchanged='{FilterCategory}'");
            return;
        }
        FilterCategory = newCat;
        Logger.Log($"SetFilterCategory: updated FilterCategory='{FilterCategory}' (load triggered by property hook)");
        await Task.CompletedTask;
    }

    // Convenience overload for programmatic callers
    public async Task ChangePeriod(PeriodType period)
    {
        TimePeriod = period;
        Logger.Log($"Caller: ChangePeriod(PeriodType) -> LoadDashboard for profileId={SelectedProfileId} period={period}");
        await LoadDashboardCommand.ExecuteAsync(SelectedProfileId);
    }

    [RelayCommand]
    public void ShowCustomPeriod()
    {
        // Only switch UI to show date pickers; don't reload until Apply
        TimePeriod = PeriodType.Custom;
    }

    [RelayCommand]
    public async Task ApplyCustomPeriod()
    {
        TimePeriod = PeriodType.Custom;
        Logger.Log($"Caller: ApplyCustomPeriod -> LoadDashboard for profileId={SelectedProfileId}");
        await LoadDashboardCommand.ExecuteAsync(SelectedProfileId);
    }

    // Called by the generated property change hook when TimePeriod changes
    partial void OnTimePeriodChanged(PeriodType value)
    {
        UpdateDatesForPeriod(value);
    }

    // Called when the SelectedFilter (bound from the UI) changes
    partial void OnSelectedFilterChanged(string value)
    {
        try
        {
            Logger.Log($"OnSelectedFilterChanged: value='{value}' currentFilterType={FilterType}");
            if (string.IsNullOrWhiteSpace(value)) return;
            // Parse the selected filter and only reload if it actually changes
            if (!Enum.TryParse<TransactionFilter>(value, ignoreCase: true, out var parsed))
            {
                parsed = TransactionFilter.All;
            }
            if (parsed == FilterType) return; // no change -> avoid reload loop
            FilterType = parsed;
            // reload dashboard once for the new filter
            Logger.Log($"Caller: OnSelectedFilterChanged parsed={parsed} -> LoadDashboard for profileId={SelectedProfileId}");
            _ = LoadDashboardCommand.ExecuteAsync(SelectedProfileId);
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "OnSelectedFilterChanged");
        }
    }

    private void UpdateDatesForPeriod(PeriodType period)
    {
        var today = DateTime.Now.Date;
        switch (period)
        {
            case PeriodType.Day:
                CustomStartDate = today;
                CustomEndDate = today;
                break;
            case PeriodType.Week:
                var wkStart = today.AddDays(-(int)today.DayOfWeek).Date;
                CustomStartDate = wkStart;
                CustomEndDate = today;
                break;
            case PeriodType.Month:
                var mStart = new DateTime(today.Year, today.Month, 1);
                CustomStartDate = mStart;
                CustomEndDate = mStart.AddMonths(1).AddDays(-1);
                break;
            case PeriodType.Year:
            case PeriodType.CalendarYear:
                var yStart = new DateTime(today.Year, 1, 1);
                CustomStartDate = yStart;
                CustomEndDate = new DateTime(today.Year, 12, 31);
                break;
            case PeriodType.FiscalYear:
            {
                var startYear = today.Month >= FiscalYearStartMonth ? today.Year : today.Year - 1;
                var fyStart = new DateTime(startYear, FiscalYearStartMonth, 1);
                var fyEnd = fyStart.AddYears(1).AddDays(-1);
                CustomStartDate = fyStart;
                CustomEndDate = fyEnd;
                break;
            }
            case PeriodType.Custom:
                // leave CustomStartDate/CustomEndDate as the user set them
                break;
            default:
                break;
        }
    }

    private bool IsInSelectedPeriod(DateTime date)
    {
        var today = DateTime.Now;
        switch (TimePeriod)
        {
            case PeriodType.Day:
                return date.Date == today.Date;
            case PeriodType.Week:
                return date >= today.AddDays(-(int)today.DayOfWeek) && date <= today;
            case PeriodType.Month:
                return date.Year == today.Year && date.Month == today.Month;
            case PeriodType.Year:
                return date.Year == today.Year;
            case PeriodType.CalendarYear:
                return date.Year == today.Year;
            case PeriodType.FiscalYear:
            {
                var startYear = today.Month >= FiscalYearStartMonth ? today.Year : today.Year - 1;
                var fyStart = new DateTime(startYear, FiscalYearStartMonth, 1);
                var fyEnd = fyStart.AddYears(1).AddDays(-1);
                return date.Date >= fyStart.Date && date.Date <= fyEnd.Date;
            }
            case PeriodType.Custom:
                return date.Date >= CustomStartDate.Date && date.Date <= CustomEndDate.Date;
            default:
                return true;
        }
    }

    private bool MatchesFilter(TransactionDto t)
    {
        if (t == null) return false;
        switch (FilterType)
        {
            case TransactionFilter.All:
                return true;
            case TransactionFilter.Expenses:
                return t.Type == CategoryType.Expense;
            case TransactionFilter.Income:
                return t.Type == CategoryType.Income;
            case TransactionFilter.Category:
                if (string.IsNullOrWhiteSpace(FilterCategory)) return true;
                return string.Equals(t.CategoryName, FilterCategory, StringComparison.OrdinalIgnoreCase);
            default:
                return true;
        }
    }

    private void CalculateSummaries()
    {
        // Use the full filtered set (_allTransactions) so totals reflect all matching items, not only the currently loaded page
        TotalIncome = _allTransactions.Where(t => t.Type == CategoryType.Income).Sum(t => t.Amount);
        TotalExpenses = _allTransactions.Where(t => t.Type == CategoryType.Expense).Sum(t => t.Amount);
        NetBalance = TotalIncome - TotalExpenses;

        // Build expense summary by category from the full set
        ExpenseSummary = _allTransactions
            .Where(t => t.Type == CategoryType.Expense)
            .GroupBy(t => t.CategoryName)
            .Select(g => new CategorySummary
            {
                Category = g.Key,
                    Amount = g.Sum(t => t.Amount),
                    Count = g.Count()
            })
            .OrderByDescending(s => s.Amount)
            .ToList();

        // Build income summary by category from the full set
        IncomeSummary = _allTransactions
            .Where(t => t.Type == CategoryType.Income)
            .GroupBy(t => t.CategoryName)
            .Select(g => new CategorySummary
            {
                Category = g.Key,
                Amount = g.Sum(t => t.Amount),
                Count = g.Count()
            })
            .OrderByDescending(s => s.Amount)
            .ToList();

        Logger.Log($"CalculateSummaries: AllTransactions={_allTransactions.Count} LoadedItems={Transactions.Count} Income={TotalIncome} Expense={TotalExpenses} Net={NetBalance}");
    }

    private async Task LoadPageAsync(bool reset = false)
    {
        try
        {
            if (reset)
            {
                Transactions.Clear();
                _currentPage = 0;
            }

            // compute page slice
            var start = _currentPage * _pageSize;
            var page = _allTransactions.Skip(start).Take(_pageSize).ToList();
            foreach (var t in page)
            {
                Transactions.Add(t);
                Logger.Log($"  Tx: Id={t.Id} Profile={t.ProfileId} Cat={t.CategoryName} Type={t.Type} Amount={t.Amount} Date={t.Date}");
            }

            _currentPage++;
            HasMore = _allTransactions.Count > _currentPage * _pageSize;
            Logger.Log($"LoadPage: page={_currentPage} pageSize={_pageSize} added={page.Count} hasMore={HasMore}");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Error loading page");
        }
        await Task.CompletedTask;
    }

    [RelayCommand]
    public async Task LoadMoreTransactions()
    {
        if (!HasMore || IsLoading) return;
        IsLoading = true;
        try
        {
            await LoadPageAsync(reset: false);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RemoveTransaction(int transactionId)
    {
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            Logger.Log($"RemoveTransaction: attempting delete id={transactionId}");
            var ok = await _apiService.DeleteTransactionAsync(transactionId);
            if (!ok)
            {
                Logger.Log($"RemoveTransaction: delete failed for id={transactionId}");
                return;
            }

            // Remove from backing list
            var removed = _allTransactions.FirstOrDefault(t => t.Id == transactionId);
            if (removed != null)
            {
                _allTransactions.Remove(removed);
            }

            // Remove from visible collection
            var inUi = Transactions.FirstOrDefault(t => t.Id == transactionId);
            if (inUi != null)
            {
                Transactions.Remove(inUi);
            }

            // If there is room on the current page, try to append more items
            if (Transactions.Count < _pageSize && HasMore)
            {
                await LoadPageAsync(reset: false);
            }

            // Refresh counts and summaries
            TransactionsCount = _allTransactions.Count;
            CalculateSummaries();
            Logger.Log($"RemoveTransaction: removed id={transactionId} remaining={TransactionsCount}");
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, "Error removing transaction");
        }
        finally
        {
            IsLoading = false;
        }
    }
}

 
