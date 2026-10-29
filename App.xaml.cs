using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using Forms = System.Windows.Forms;

namespace DesktopFurniture;

public sealed class Furniture
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Kind { get; set; } = "drawer";
    public string Name { get; set; } = "我的抽屉";
    public double X { get; set; } = 120;
    public double Y { get; set; } = 160;
    public bool Open { get; set; } = true;
    public List<string> Items { get; set; } = new();
}

public partial class App : Application
{
    internal readonly List<Furniture> Furniture = new();
    readonly List<FurnitureWindow> windows = new();
    Forms.NotifyIcon? tray;
    Window? warehouse;
    readonly string data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopFurniture", "layout.json");
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            if (File.Exists(data)) Furniture.AddRange(JsonSerializer.Deserialize<List<Furniture>>(File.ReadAllText(data)) ?? new());
        }
        catch (Exception ex)
        {
            MessageBox.Show("布局读取失败，原文件将保留。\n" + ex.Message);
            try { File.Copy(data, data + ".recovery-" + DateTime.Now.ToString("yyyyMMddHHmmss")); }
            catch { Shutdown(); return; }
        }
        tray = new Forms.NotifyIcon { Icon = System.Drawing.SystemIcons.Application, Text = "家具桌面 · 储物仓", Visible = true };
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("打开储物仓", null, (_, _) => Dispatcher.Invoke(ShowWarehouse));
        menu.Items.Add("显示 / 隐藏家具", null, (_, _) => Dispatcher.Invoke(() => { foreach (var w in windows) { if (w.IsVisible) w.Hide(); else w.Show(); } }));
        menu.Items.Add("退出", null, (_, _) => Dispatcher.Invoke(() => Shutdown()));
        tray.ContextMenuStrip = menu;
        tray.DoubleClick += (_, _) => Dispatcher.Invoke(ShowWarehouse);
        var entry = new Window { Width = 115, Height = 48, Left = 18, Top = 18, WindowStyle = WindowStyle.None, AllowsTransparency = true, Background = Brushes.Transparent, ShowInTaskbar = false, ResizeMode = ResizeMode.NoResize };
        var button = Ui.Button("▤  储物仓"); button.Click += (_, _) => ShowWarehouse();
        entry.Content = button; entry.Show();
        foreach (var f in Furniture) Spawn(f);
        if (Furniture.Count == 0) ShowWarehouse();
    }
    internal void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(data)!);
            File.WriteAllText(data + ".tmp", JsonSerializer.Serialize(Furniture, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(data + ".tmp", data, true);
        }
        catch (Exception ex) { MessageBox.Show("无法保存布局：" + ex.Message); }
    }
    void Spawn(Furniture f) { var w = new FurnitureWindow(this, f); windows.Add(w); w.Show(); }
    internal void Remove(Furniture f, FurnitureWindow w)
    {
        if (MessageBox.Show("移除这件家具？仅删除收纳记录，原文件不受影响。", "移除家具", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        Furniture.Remove(f); windows.Remove(w); w.Close(); Save();
    }
    internal void ShowWarehouse()
    {
        if (warehouse != null) { warehouse.Activate(); return; }
        warehouse = new Window { Title = "储物仓 · 家具桌面", Width = 520, Height = 550, Background = Ui.Cream, WindowStartupLocation = WindowStartupLocation.CenterScreen };
        var panel = new StackPanel { Margin = new Thickness(28) };
        panel.Children.Add(new TextBlock { Text = "给桌面安个家", FontSize = 29, Foreground = Ui.Ink, Margin = new Thickness(0, 0, 0, 10) });
        panel.Children.Add(new TextBlock { Text = "选择一件家具，开始收纳。每种都可以添加多件。", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 24) });
        foreach (var choice in new[] { ("rug", "▱  编织地毯", "掀开一角，把杂物收进柔软织物下"), ("drawer", "▤  木质抽屉", "拉开抽屉，收藏常用软件"), ("bowl", "◉  带盖收纳盆", "揭开圆盖，给小物件找个家") })
        {
            var b = Ui.Button(choice.Item2 + "\n" + choice.Item3); b.Margin = new Thickness(0, 0, 0, 12);
            b.Click += (_, _) => { var area = SystemParameters.WorkArea; var f = new Furniture { Kind = choice.Item1, Name = choice.Item2.Substring(3), X = area.Left + 100 + Furniture.Count % 4 * 65, Y = area.Top + 120 + Furniture.Count % 4 * 55 }; Furniture.Add(f); Spawn(f); Save(); };
            panel.Children.Add(b);
        }
        panel.Children.Add(new TextBlock { Text = "使用方法\n拖动家具顶部移动位置；点击盖子 / 把手开合。\n展开后拖入 .lnk、.exe、文件或文件夹。\n双击图标打开；右键可移出收纳或删除家具。\n\n收纳只保存引用，不移动原文件，也不自动移除桌面图标。\n托盘菜单可隐藏家具和退出。", TextWrapping = TextWrapping.Wrap, Foreground = Ui.Ink, LineHeight = 23 });
        warehouse.Content = new ScrollViewer { Content = panel };
        warehouse.Closed += (_, _) => warehouse = null;
        warehouse.Show();
    }
    protected override void OnExit(ExitEventArgs e) { Save(); tray?.Dispose(); base.OnExit(e); }
}

internal static class Ui
{
    internal static readonly Brush Cream = new SolidColorBrush(Color.FromRgb(248, 242, 230));
    internal static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(73, 53, 39));
    internal static readonly Brush Wood = new LinearGradientBrush(Color.FromRgb(199, 148, 98), Color.FromRgb(145, 96, 58), 90);
    internal static Button Button(string text) => new() { Content = text, Padding = new Thickness(15, 11, 15, 11), Background = Cream, Foreground = Ink, BorderBrush = new SolidColorBrush(Color.FromRgb(186, 151, 112)), BorderThickness = new Thickness(1), Cursor = Cursors.Hand, FontSize = 14 };
}

internal sealed class FurnitureWindow : Window
{
    readonly App app;
    readonly Furniture model;
    readonly Border tray;
    readonly Button lid;
    readonly WrapPanel items = new();
    bool animating;
    public FurnitureWindow(App app, Furniture model)
    {
        this.app = app; this.model = model;
        Width = 330; Height = 320; Left = model.X; Top = model.Y;
        var area = SystemParameters.WorkArea;
        if (Left + Width < SystemParameters.VirtualScreenLeft || Left > SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth || Top + Height < SystemParameters.VirtualScreenTop || Top > SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight) { Left = area.Left + 100; Top = area.Top + 100; }
        WindowStyle = WindowStyle.None; AllowsTransparency = true; Background = Brushes.Transparent;
        ShowInTaskbar = false; ResizeMode = ResizeMode.NoResize;
        var root = new Grid { Margin = new Thickness(12) };
        var body = new StackPanel(); root.Children.Add(body);
        var handle = new Border { Background = Ui.Wood, CornerRadius = new CornerRadius(12, 12, 0, 0), Padding = new Thickness(14, 9, 14, 9), Cursor = Cursors.SizeAll };
        handle.Child = new TextBlock { Text = model.Name + "   ·   拖动摆放", Foreground = Ui.Cream, FontSize = 12 };
        handle.MouseLeftButtonDown += (_, e) => { if (e.ButtonState == MouseButtonState.Pressed) { DragMove(); model.X = Left; model.Y = Top; app.Save(); } };
        body.Children.Add(handle);
        lid = Ui.Button(""); lid.Background = model.Kind == "rug" ? new SolidColorBrush(Color.FromRgb(141, 102, 85)) : Ui.Wood; lid.Foreground = Ui.Cream;
        lid.Height = 62; lid.BorderThickness = new Thickness(0); lid.Click += (_, _) => Toggle(); body.Children.Add(lid);
        tray = new Border { Background = new SolidColorBrush(Color.FromRgb(102, 70, 46)), BorderBrush = Ui.Wood, BorderThickness = new Thickness(9, 3, 9, 12), CornerRadius = new CornerRadius(0, 0, model.Kind == "bowl" ? 70 : 12, model.Kind == "bowl" ? 70 : 12), Height = model.Open ? 200 : 0, ClipToBounds = true, AllowDrop = true };
        if (model.Kind == "rug") { tray.Background = new SolidColorBrush(Color.FromRgb(193, 156, 132)); tray.BorderBrush = new SolidColorBrush(Color.FromRgb(116, 76, 61)); }
        tray.Child = new ScrollViewer { Content = items, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        tray.DragOver += (_, e) => { e.Effects = model.Open && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Link : DragDropEffects.None; e.Handled = true; };
        tray.Drop += (_, e) => { if (!model.Open || !e.Data.GetDataPresent(DataFormats.FileDrop)) return; foreach (var path in (string[])e.Data.GetData(DataFormats.FileDrop)) if ((File.Exists(path) || Directory.Exists(path)) && !model.Items.Contains(path, StringComparer.OrdinalIgnoreCase)) model.Items.Add(path); app.Save(); RenderItems(); e.Handled = true; };
        body.Children.Add(tray);
        var context = new ContextMenu();
        var rename = new MenuItem { Header = "重命名家具" }; rename.Click += (_, _) => Rename(); context.Items.Add(rename);
        var remove = new MenuItem { Header = "移除家具（保留原文件）" }; remove.Click += (_, _) => app.Remove(model, this); context.Items.Add(remove);
        handle.ContextMenu = context;
        Content = root; RenderItems(); UpdateLabel();
    }
    void Rename()
    {
        var dialog = new Window { Title = "重命名", Width = 300, Height = 160, Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize };
        var p = new StackPanel { Margin = new Thickness(14) }; var input = new TextBox { Text = model.Name, Margin = new Thickness(0, 0, 0, 12) }; p.Children.Add(input);
        var b = Ui.Button("保存"); b.Click += (_, _) => { if (string.IsNullOrWhiteSpace(input.Text)) return; model.Name = input.Text.Trim(); ((TextBlock)((Border)((StackPanel)((Grid)Content).Children[0]).Children[0]).Child).Text = model.Name + "   ·   拖动摆放"; app.Save(); dialog.Close(); }; p.Children.Add(b); dialog.Content = p; dialog.ShowDialog();
    }
    void UpdateLabel() => lid.Content = model.Kind switch { "rug" => model.Open ? "▱  放下地毯  ·  " + model.Items.Count : "▱  掀开地毯", "bowl" => model.Open ? "◉  盖上盖子  ·  " + model.Items.Count : "◉  揭开盖子", _ => model.Open ? "━━━━  推回抽屉  ·  " + model.Items.Count : "━━━━  拉开抽屉" };
    void Toggle()
    {
        if (animating) return; animating = true; model.Open = !model.Open;
        var animation = new DoubleAnimation(model.Open ? 200 : 0, TimeSpan.FromMilliseconds(280)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut } };
        animation.Completed += (_, _) => { tray.BeginAnimation(HeightProperty, null); tray.Height = model.Open ? 200 : 0; animating = false; };
        tray.BeginAnimation(HeightProperty, animation); UpdateLabel(); app.Save();
    }
    void RenderItems()
    {
        items.Children.Clear();
        if (model.Items.Count == 0) items.Children.Add(new TextBlock { Text = "拖入软件快捷方式\n或文件、文件夹", Foreground = Ui.Cream, Margin = new Thickness(30), TextAlignment = TextAlignment.Center, FontSize = 14 });
        foreach (var path in model.Items.ToArray())
        {
            var p = new StackPanel { Width = 75, Margin = new Thickness(5), ToolTip = path };
            try { using var icon = System.Drawing.Icon.ExtractAssociatedIcon(path); if (icon != null) p.Children.Add(new Image { Source = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()), Width = 32, Height = 32 }); }
            catch { p.Children.Add(new TextBlock { Text = Directory.Exists(path) ? "▰" : "◇", FontSize = 26, Foreground = Ui.Cream, HorizontalAlignment = HorizontalAlignment.Center }); }
            p.Children.Add(new TextBlock { Text = Path.GetFileNameWithoutExtension(path), TextTrimming = TextTrimming.CharacterEllipsis, TextAlignment = TextAlignment.Center, Foreground = Ui.Cream, FontSize = 11, Margin = new Thickness(0, 4, 0, 0) });
            p.MouseLeftButtonDown += (_, e) => { if (e.ClickCount == 2) { try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show("无法打开，请检查原文件是否仍存在。\n" + ex.Message); } e.Handled = true; } };
            var menu = new ContextMenu(); var remove = new MenuItem { Header = "移出收纳（保留原文件）" }; remove.Click += (_, _) => { model.Items.Remove(path); app.Save(); RenderItems(); }; menu.Items.Add(remove);
            foreach (var target in app.Furniture.Where(f => f.Id != model.Id))
            {
                var move = new MenuItem { Header = "移入：" + target.Name };
                move.Click += (_, _) => { if (!target.Items.Contains(path)) target.Items.Add(path); model.Items.Remove(path); app.Save(); foreach (Window w in app.Windows) if (w is FurnitureWindow fw) fw.RenderItems(); };
                menu.Items.Add(move);
            }
            p.ContextMenu = menu; items.Children.Add(p);
        }
        UpdateLabel();
    }
}
