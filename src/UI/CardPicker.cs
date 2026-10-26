using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace DefectCompanion
{
    public sealed class CardPicker : Window
    {
        private readonly Border panel;
        private readonly TextBlock detail;
        private readonly Action<string> play;
        private readonly UniformGrid cards;
        private readonly TextBlock pageLabel;
        private readonly Button previous, next;
        private readonly TextBox search;
        private readonly StackPanel categories;
        private string category = "全部";
        private int page;
        public int PageIndex { get { return page; } }
        public int FilteredCount { get; private set; }
        public CardInfo[] VisibleCards { get; private set; }
        private bool closing;
        public CardPicker(PetWindow pet, Action<string> onPlay)
        {
            play = onPlay;
            Owner = pet; Title = "故障机器人 · 选择卡牌";
            WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true; Background = Brushes.Transparent;
            ShowInTaskbar = false; Topmost = pet.Topmost;
            FontFamily = new FontFamily("Microsoft YaHei UI");
            Width = 864; Height = 590;
            var area = SystemParameters.WorkArea;
            double scale = Math.Min(1, Math.Min((area.Width - 24) / Width, (area.Height - 24) / Height));
            Width *= scale; Height *= scale;
            Left = Math.Max(area.Left + 8, Math.Min(area.Right - Width - 8, pet.Left + pet.Width / 2 - Width / 2));
            Top = Math.Max(area.Top + 8, Math.Min(area.Bottom - Height - 8, pet.Top - Height + 130));
            panel = new Border {
                Width = 864, Height = 590, CornerRadius = new CornerRadius(18), Padding = new Thickness(12),
                Background = new SolidColorBrush(Color.FromArgb(248, 20, 27, 39)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(115, 133, 148)), BorderThickness = new Thickness(1)
            };
            Content = new Viewbox { Child = panel, Stretch = Stretch.Uniform };
            var grid = new Grid(); panel.Child = grid;
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(38) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(36) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(386) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(104) });
            var title = new TextBlock { Text = "选择卡牌 · 36 张", FontSize = 18, Foreground = new SolidColorBrush(Color.FromRgb(240, 230, 202)), Margin = new Thickness(10, 4, 0, 0) };
            grid.Children.Add(title);
            var cancel = new TextBlock { Text = "右键 / Esc 收起", FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(163, 182, 194)), HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 13, 0) };
            grid.Children.Add(cancel);
            search = new TextBox { Width = 235, Height = 27, FontSize = 13, Padding = new Thickness(7, 3, 7, 3), VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(235, 1, 0, 0), Background = new SolidColorBrush(Color.FromRgb(34, 46, 61)), Foreground = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(79, 110, 127)), ToolTip = "搜索卡牌名称" };
            AutomationProperties.SetName(search, "搜索卡牌名称"); grid.Children.Add(search);
            categories = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(9, 0, 0, 0) };
            Grid.SetRow(categories, 1); grid.Children.Add(categories);
            foreach (string value in new [] { "全部", "充能球", "攻击", "能力", "机能" })
            {
                string choice = value; int count = value == "全部" ? GameArt.Cards.Length : GameArt.Cards.Count(c => c.Category == value);
                var tab = SmallButton(value + "  " + count); tab.Tag = value; tab.Click += delegate { SetCategory(choice); }; categories.Children.Add(tab);
            }
            cards = new UniformGrid { Columns = 6, Rows = 2, ClipToBounds = true };
            Grid.SetRow(cards, 2); grid.Children.Add(cards);
            var footer = new Grid(); Grid.SetRow(footer, 3); grid.Children.Add(footer);
            Panel.SetZIndex(footer,20);
            footer.RowDefinitions.Add(new RowDefinition{Height=new GridLength(43)});
            footer.RowDefinitions.Add(new RowDefinition{Height=new GridLength(1,GridUnitType.Star)});
            var navigation = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
            previous = SmallButton("‹ 上一页"); previous.Click += delegate { MovePage(-1); }; navigation.Children.Add(previous);
            AutomationProperties.SetName(previous,"上一页");
            pageLabel = new TextBlock { FontSize = 13, Foreground = Brushes.LightCyan, Margin = new Thickness(8, 7, 8, 0) }; navigation.Children.Add(pageLabel);
            next = SmallButton("下一页 ›"); next.Click += delegate { MovePage(1); }; navigation.Children.Add(next); footer.Children.Add(navigation);
            AutomationProperties.SetName(next,"下一页");
            detail = new TextBlock { Text = "左键出牌 · 悬停查看桌宠效果", TextWrapping = TextWrapping.Wrap, FontSize = 13, LineHeight = 21, Margin = new Thickness(12, 0, 12, 0), IsHitTestVisible=false,Foreground = new SolidColorBrush(Color.FromRgb(207, 221, 222)) };
            Grid.SetRow(detail,1);
            footer.Children.Add(detail);
            var searchHint = new TextBlock { Text = "输入名称搜索 · ← → 翻页", FontSize = 12, Foreground = Brushes.LightSlateGray, HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Center,IsHitTestVisible=false,Margin = new Thickness(12, 0, 0, 0) }; footer.Children.Add(searchHint);
            search.TextChanged += delegate { page = 0; RefreshCards(); };
            RefreshCards();
            Closing += delegate { closing = true; };
            PreviewMouseRightButtonDown += delegate(object sender, MouseButtonEventArgs e) { e.Handled = true; CancelSelection(); };
            KeyDown += delegate(object sender, KeyEventArgs e) {
                if (e.Key == Key.Escape) { e.Handled = true; CancelSelection(); }
                else if (!search.IsKeyboardFocusWithin && (e.Key == Key.Left || e.Key == Key.Right)) { MovePage(e.Key == Key.Left ? -1 : 1); e.Handled = true; }
            };
            Deactivated += delegate { CancelSelection(); };
        }
        private static Button SmallButton(string text)
        {
            var button = new Button { Content = text, Cursor = Cursors.Hand, FontSize = 12, Padding = new Thickness(11, 5, 11, 5), Margin = new Thickness(0, 0, 7, 4),
                Background = new SolidColorBrush(Color.FromRgb(37, 53, 69)), Foreground = Brushes.LightCyan, BorderBrush = new SolidColorBrush(Color.FromRgb(69, 96, 113)) };
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
            var content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(content); template.VisualTree = border; button.Template = template;
            button.IsEnabledChanged += delegate { button.Opacity = button.IsEnabled ? 1 : .35; };
            return button;
        }
        public void SetCategory(string value) { category = value; page = 0; RefreshCards(); }
        public void SetQuery(string value) { search.Text = value; }
        public void MovePage(int delta) { page += delta; RefreshCards(); }
        private void RefreshCards()
        {
            var filtered = GameArt.Cards.Where(c => (category == "全部" || c.Category == category) && (c.Name.IndexOf(search.Text.Trim(), StringComparison.OrdinalIgnoreCase) >= 0 || c.Skill.IndexOf(search.Text.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
            FilteredCount = filtered.Length; int pages = Math.Max(1, (filtered.Length + 11) / 12);
            page = Math.Max(0, Math.Min(pages - 1, page)); VisibleCards = filtered.Skip(page * 12).Take(12).ToArray();
            pageLabel.Text = (page + 1) + " / " + pages + "  ·  " + filtered.Length + " 张";
            previous.IsEnabled = page > 0; next.IsEnabled = page < pages - 1;
            foreach (Button tab in categories.Children) tab.Background = new SolidColorBrush((string)tab.Tag == category ? Color.FromRgb(55, 103, 122) : Color.FromRgb(37, 53, 69));
            detail.Text = filtered.Length == 0 ? "没有找到这张卡牌，试试更短的名称。" : "左键出牌 · 悬停查看桌宠效果 · 右键取消";
            cards.Children.Clear();
            foreach (var info in VisibleCards)
            {
                CardInfo card = info;
                var image = new Image { Source = GameArt.Bitmap("card-" + card.Asset), Width = 128, Height = 176, Stretch = Stretch.Uniform };
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
                var button = new Button { Content = image, Tag = card, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Margin = new Thickness(4, 6, 4, 6), Cursor = Cursors.Hand,
                    RenderTransformOrigin = new Point(.5, .5), RenderTransform = new ScaleTransform(1, 1) };
                var template = new ControlTemplate(typeof(Button));
                template.VisualTree = new FrameworkElementFactory(typeof(ContentPresenter));
                button.Template = template;
                AutomationProperties.SetName(button, card.Name + "。桌宠效果：" + card.CompanionEffect);
                button.Click += delegate { CancelSelection(); play(card.Skill); };
                button.MouseEnter += delegate { Panel.SetZIndex(button, 5); HoverCard(button, true); detail.Text = card.Name + "  ·  " + card.CompanionEffect; };
                button.MouseLeave += delegate { Panel.SetZIndex(button, 0); HoverCard(button, false); };
                button.GotKeyboardFocus += delegate { detail.Text = card.Name + "  ·  " + card.CompanionEffect; HoverCard(button, true); };
                button.LostKeyboardFocus += delegate { HoverCard(button, false); };
                cards.Children.Add(button);
            }
        }
        public void CancelSelection()
        {
            // Deactivated also fires during Close; the first close must own the whole teardown.
            if (closing) return;
            closing = true;
            Close();
        }
        private static void HoverCard(Button button, bool hover)
        {
            var transform = (ScaleTransform)button.RenderTransform;
            var animation = new DoubleAnimation(hover ? 1.15 : 1, TimeSpan.FromMilliseconds(120)) { EasingFunction = new QuadraticEase() };
            transform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            transform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
            button.Effect = hover ? new DropShadowEffect { Color = Color.FromRgb(107, 218, 255), BlurRadius = 16, ShadowDepth = 0, Opacity = .5 } : null;
        }
        public void SavePreview(string path)
        {
            panel.Measure(new Size(864, 590)); panel.Arrange(new Rect(0, 0, 864, 590)); panel.UpdateLayout();
            var bitmap = new RenderTargetBitmap(864, 590, 96, 96, PixelFormats.Pbgra32); bitmap.Render(panel);
            var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
            using (var file = File.Create(path)) png.Save(file);
        }
    }
}
