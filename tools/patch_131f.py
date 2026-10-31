"""Add lucide icon into rail item visual tree (missing piece)."""
import io

p = "app/src/SettingsWindow.cs"
s = io.open(p, encoding="utf-8").read()
old = """            var sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            var t = Glass.Label(label, 13, false, true);
            t.Margin = new Thickness(11, 0, 0, 0);
            t.VerticalAlignment = VerticalAlignment.Center;
            sp.Children.Add(t);
            item.Child = sp;"""
if old in s:
    new = """            var sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            var ic = LucideHost.Build(glyph, 15, false);
            var icHost = new ContentControl();
            icHost.Content = ic;
            icHost.Width = 16; icHost.Height = 16;
            icHost.VerticalAlignment = VerticalAlignment.Center;
            _railGlyphs.Add(icHost);
            sp.Children.Add(icHost);
            var t = Glass.Label(label, 13, false, true);
            t.Margin = new Thickness(11, 0, 0, 0);
            t.VerticalAlignment = VerticalAlignment.Center;
            sp.Children.Add(t);
            item.Child = sp;"""
    s = s.replace(old, new, 1)
    io.open(p, "w", encoding="utf-8").write(s)
    print("rail icon added")
else:
    print("rail already has icon")
