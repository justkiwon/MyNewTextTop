using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using TextTop.Desktop.ViewModels;

namespace TextTop.Desktop.Views;

public partial class MemoWindow : Window
{
    private readonly MemoWindowViewModel _viewModel;
    private bool _isClosingSaveRunning;
    private readonly List<int> _secondaryCarets = [];

    public MemoWindow(MemoWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void Topmost_Changed(object sender, RoutedEventArgs e)
    {
        Topmost = _viewModel.IsTopMost;
        // This intentionally does not save. Topmost is persisted only on SAVE or window closing.
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.CaptureWindowBounds(Left, Top, Width, Height);

        try
        {
            var result = await _viewModel.SaveAsync();
            if (result.Success)
            {
                MessageBox.Show("Supabase 저장 성공. Refresh/Load 목록에서 확인할 수 있습니다.", "TextTop", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (result.IsConflict)
            {
                MessageBox.Show("저장 충돌입니다. 다른 곳에서 먼저 수정되어 자동 덮어쓰지 않았고, 현재 내용은 로컬 캐시에 보존했습니다.", "TextTop", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else if (result.IsOffline)
            {
                MessageBox.Show($"Supabase에는 저장되지 않았습니다. 현재 내용은 로컬 캐시에 저장했습니다.\n\n{result.ErrorMessage}", "TextTop", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show($"저장 실패: {result.ErrorMessage}", "TextTop", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"저장 중 오류가 발생했습니다.\n{ex.Message}", "TextTop", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MemoContentBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (Keyboard.Modifiers != (ModifierKeys.Control | ModifierKeys.Alt))
        {
            return;
        }

        var point = e.GetPosition(MemoContentBox);
        var index = MemoContentBox.GetCharacterIndexFromPoint(point, true);
        if (index < 0)
        {
            index = MemoContentBox.Text.Length;
        }

        if (!_secondaryCarets.Contains(index))
        {
            _secondaryCarets.Add(index);
            _secondaryCarets.Sort();
        }

        _viewModel.StatusText = $"{_secondaryCarets.Count} secondary cursors. Esc clears them.";
        e.Handled = true;
    }

    private void MemoContentBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (_secondaryCarets.Count == 0)
        {
            return;
        }

        InsertAtAllCarets(e.Text);
        e.Handled = true;
    }

    private void MemoContentBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _secondaryCarets.Count > 0)
        {
            _secondaryCarets.Clear();
            _viewModel.StatusText = "Secondary cursors cleared.";
            e.Handled = true;
            return;
        }

        if (_secondaryCarets.Count == 0)
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            InsertAtAllCarets(Environment.NewLine);
            e.Handled = true;
        }
        else if (e.Key == Key.Back)
        {
            RemoveAtAllCarets(removeBeforeCaret: true);
            e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            RemoveAtAllCarets(removeBeforeCaret: false);
            e.Handled = true;
        }
    }

    private void InsertAtAllCarets(string text)
    {
        var mainCaret = MemoContentBox.CaretIndex;
        var positions = _secondaryCarets
            .Append(mainCaret)
            .Distinct()
            .Where(i => i >= 0 && i <= MemoContentBox.Text.Length)
            .OrderByDescending(i => i)
            .ToList();

        var content = MemoContentBox.Text;
        foreach (var position in positions)
        {
            content = content.Insert(position, text);
        }

        MemoContentBox.Text = content;
        MemoContentBox.CaretIndex = mainCaret + text.Length;
        _secondaryCarets.Clear();
        foreach (var position in positions.Where(i => i != mainCaret).OrderBy(i => i))
        {
            _secondaryCarets.Add(position + text.Length);
        }
        UpdateMemoContentSource();
    }

    private void RemoveAtAllCarets(bool removeBeforeCaret)
    {
        var mainCaret = MemoContentBox.CaretIndex;
        var positions = _secondaryCarets
            .Append(mainCaret)
            .Distinct()
            .OrderByDescending(i => i)
            .ToList();

        var content = MemoContentBox.Text;
        var nextCarets = new List<int>();

        foreach (var caret in positions)
        {
            var removeIndex = removeBeforeCaret ? caret - 1 : caret;
            if (removeIndex < 0 || removeIndex >= content.Length)
            {
                nextCarets.Add(caret);
                continue;
            }

            content = content.Remove(removeIndex, 1);
            nextCarets.Add(removeBeforeCaret ? caret - 1 : caret);
        }

        MemoContentBox.Text = content;
        MemoContentBox.CaretIndex = Math.Clamp(removeBeforeCaret ? mainCaret - 1 : mainCaret, 0, MemoContentBox.Text.Length);
        _secondaryCarets.Clear();
        foreach (var caret in nextCarets.Where(i => i != MemoContentBox.CaretIndex).Select(i => Math.Clamp(i, 0, MemoContentBox.Text.Length)).Distinct().OrderBy(i => i))
        {
            _secondaryCarets.Add(caret);
        }
        UpdateMemoContentSource();
    }

    private void UpdateMemoContentSource()
    {
        MemoContentBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
    }

    private async void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isClosingSaveRunning)
        {
            return;
        }

        e.Cancel = true;
        _isClosingSaveRunning = true;

        try
        {
            _viewModel.CaptureWindowBounds(Left, Top, Width, Height);
            await _viewModel.SaveAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"닫는 중 저장에 실패했지만 앱은 종료됩니다.\n{ex.Message}", "TextTop", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            e.Cancel = false;
            Close();
        }
    }
}
