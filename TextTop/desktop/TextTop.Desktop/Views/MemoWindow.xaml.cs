using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using TextTop.Desktop.ViewModels;

namespace TextTop.Desktop.Views;

public partial class MemoWindow : Window
{
    private readonly MemoWindowViewModel _viewModel;
    private bool _isClosingSaveRunning;
    private bool _isLoadingContent;

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

    private void StrikethroughButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleStrikethrough();
    }

    private void MemoContentBox_Loaded(object sender, RoutedEventArgs e)
    {
        LoadContentIntoEditor(_viewModel.Content);
    }

    private void MemoContentBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isLoadingContent)
        {
            return;
        }

        _viewModel.Content = SerializeEditorContent();
    }

    private void MemoContentBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.X)
        {
            ToggleStrikethrough();
            e.Handled = true;
        }
    }

    private void ToggleStrikethrough()
    {
        var selection = MemoContentBox.Selection;
        var shouldRemove = IsSelectionStruck(selection);
        selection.ApplyPropertyValue(Inline.TextDecorationsProperty, shouldRemove ? null : TextDecorations.Strikethrough);
        MemoContentBox.Focus();
        _viewModel.Content = SerializeEditorContent();
    }

    private static bool IsSelectionStruck(TextRange selection)
    {
        var value = selection.GetPropertyValue(Inline.TextDecorationsProperty);
        return value is TextDecorationCollection decorations
            && decorations.Any(decoration => decoration.Location == TextDecorationLocation.Strikethrough);
    }

    private void LoadContentIntoEditor(string content)
    {
        _isLoadingContent = true;
        try
        {
            var document = new FlowDocument();
            var paragraph = new Paragraph();
            document.Blocks.Add(paragraph);

            var html = LooksLikeHtml(content)
                ? content
                : Regex.Replace(WebUtility.HtmlEncode(content).Replace("\r\n", "\n").Replace("\n", "<br>"), "~~(.+?)~~", "<s>$1</s>");

            var strikeDepth = 0;
            foreach (var token in Regex.Split(html, "(<[^>]+>)"))
            {
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }

                if (token.StartsWith("<", StringComparison.Ordinal))
                {
                    var tag = token.Trim().ToLowerInvariant();
                    if (tag.StartsWith("<s") || tag.StartsWith("<strike") || tag.StartsWith("<del"))
                    {
                        strikeDepth++;
                    }
                    else if (tag.StartsWith("</s") || tag.StartsWith("</strike") || tag.StartsWith("</del"))
                    {
                        strikeDepth = Math.Max(0, strikeDepth - 1);
                    }
                    else if (tag.StartsWith("<br") || tag.StartsWith("</div") || tag.StartsWith("</p"))
                    {
                        paragraph.Inlines.Add(new LineBreak());
                    }

                    continue;
                }

                AddTextRun(paragraph, WebUtility.HtmlDecode(token), strikeDepth > 0);
            }

            MemoContentBox.Document = document;
        }
        finally
        {
            _isLoadingContent = false;
        }
    }

    private static void AddTextRun(Paragraph paragraph, string text, bool isStruck)
    {
        if (text.Length == 0)
        {
            return;
        }

        var run = new Run(text);
        if (isStruck)
        {
            run.TextDecorations = TextDecorations.Strikethrough;
        }

        paragraph.Inlines.Add(run);
    }

    private string SerializeEditorContent()
    {
        var builder = new StringBuilder();

        foreach (var block in MemoContentBox.Document.Blocks)
        {
            if (block is Paragraph paragraph)
            {
                AppendInlines(builder, paragraph.Inlines);
            }
        }

        return builder.ToString();
    }

    private static void AppendInlines(StringBuilder builder, InlineCollection inlines)
    {
        foreach (var inline in inlines)
        {
            switch (inline)
            {
                case Run run:
                    AppendRun(builder, run);
                    break;
                case LineBreak:
                    builder.Append("<br>");
                    break;
                case Span span:
                    AppendInlines(builder, span.Inlines);
                    break;
            }
        }
    }

    private static void AppendRun(StringBuilder builder, Run run)
    {
        var text = WebUtility.HtmlEncode(run.Text);
        var isStruck = run.TextDecorations.Any(decoration => decoration.Location == TextDecorationLocation.Strikethrough);
        if (isStruck)
        {
            builder.Append("<s>");
        }

        builder.Append(text);

        if (isStruck)
        {
            builder.Append("</s>");
        }
    }

    private static bool LooksLikeHtml(string content)
    {
        return Regex.IsMatch(content, @"</?[a-z][\s\S]*>", RegexOptions.IgnoreCase);
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
