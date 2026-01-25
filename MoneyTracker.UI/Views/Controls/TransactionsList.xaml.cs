using Microsoft.Maui.Controls;

namespace MoneyTracker.UI.Views.Controls;

public partial class TransactionsList : ContentView
{
    public TransactionsList()
    {
        InitializeComponent();
    }
    // track open confirm buttons so taps outside can hide them
    private readonly List<Button> _openConfirmButtons = new();
    private void OnIndicatorTapped(object? sender, EventArgs e)
    {
        try
        {
            if (sender is not Button btn) return;
            // parent grid holds the item controls
            if (btn.Parent is not Grid parentGrid) return;

            // find confirm button in the same template (the remove button)
            var confirmBtn = parentGrid.Children.OfType<Button>().FirstOrDefault(b => b.Text == "Remove");
            if (confirmBtn != null)
            {
                // toggle confirm visibility
                var willShow = !confirmBtn.IsVisible;
                confirmBtn.IsVisible = willShow;

                // maintain tracking list
                if (willShow)
                {
                    if (!_openConfirmButtons.Contains(confirmBtn)) _openConfirmButtons.Add(confirmBtn);
                }
                else
                {
                    _openConfirmButtons.Remove(confirmBtn);
                }

                // toggle indicator background between red and white
                btn.BackgroundColor = willShow ? Microsoft.Maui.Graphics.Color.FromArgb("#D9534F") : Microsoft.Maui.Graphics.Colors.White;
            }
        }
        catch (Exception ex)
        {
            MoneyTracker.UI.Services.Logger.LogException(ex, "OnIndicatorTapped error");
        }
    }

    private void OnItemGridTapped(object? sender, EventArgs e)
    {
        try
        {
            Frame? frame = null;

            // sender may be the TapGestureRecognizer or the Frame depending on runtime
            if (sender is Frame f)
                frame = f;
            else if (sender is TapGestureRecognizer tg)
                frame = tg.Parent as Frame;

            if (frame == null) return;

            if (frame == null) return;

            var parentGrid = frame.Content as Grid;
            if (parentGrid == null) return;

            var confirmBtn = parentGrid.Children.OfType<Button>().FirstOrDefault(b => b.Text == "Remove");
            // hide any open confirm buttons tracked
            foreach (var cb in _openConfirmButtons.ToList())
            {
                try
                {
                    cb.IsVisible = false;
                    // try to reset sibling indicator color
                    if (cb.Parent is Grid g)
                    {
                        var indicatorBtn = g.Children.OfType<Button>().FirstOrDefault(b => b != cb && b.Text != "Remove");
                        if (indicatorBtn != null)
                        {
                            indicatorBtn.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#D9534F");
                        }
                    }
                }
                catch { }
            }
            _openConfirmButtons.Clear();
        }
        catch (Exception ex)
        {
            MoneyTracker.UI.Services.Logger.LogException(ex, "OnItemGridTapped error");
        }
    }

    private async void OnConfirmRemoveClicked(object? sender, EventArgs e)
    {
        try
        {
            if (sender is not Button btn) return;
            if (btn.Parent is not Grid parentGrid) return;

            // get the item's binding context from the parent grid's BindingContext
            var bc = parentGrid.BindingContext ?? btn.BindingContext;
            if (bc == null)
            {
                // sometimes the BindingContext is on the parent Frame -> walk up
                var frame = parentGrid.Parent as Microsoft.Maui.Controls.Frame;
                bc = frame?.BindingContext;
            }
            if (bc == null) return;

            var idProp = bc.GetType().GetProperty("Id");
            if (idProp == null) return;
            var idObj = idProp.GetValue(bc);
            if (idObj == null) return;
            if (!int.TryParse(idObj.ToString(), out var txId)) return;

            if (BindingContext is not MoneyTracker.UI.ViewModels.MainViewModel mainVm) return;
            var dashboard = mainVm.Dashboard;
            if (dashboard == null) return;

            await dashboard.RemoveTransactionCommand.ExecuteAsync(txId);

            // reset UI: hide confirm button and reset any slider in this template
            btn.IsVisible = false;
            _openConfirmButtons.Remove(btn);
            // reset the indicator button (sibling) to white
            var indicator = parentGrid.Children.OfType<Button>().FirstOrDefault(b => b != btn && b.Text != "Remove");
            if (indicator != null)
            {
                indicator.BackgroundColor = Microsoft.Maui.Graphics.Colors.White;
            }
        }
        catch (Exception ex)
        {
            MoneyTracker.UI.Services.Logger.LogException(ex, "OnConfirmRemoveClicked error");
        }
    }

    

}
