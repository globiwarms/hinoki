"""Fix FindTitle InvalidCastException: keep a direct reference to the header title."""
import io

p = "app/src/SettingsWindow.cs"
s = io.open(p, encoding="utf-8").read()

# 1) add field
s = s.replace("        ContentControl _pageHost;",
              "        ContentControl _pageHost;\n        TextBlock _pageTitle;")

# 2) assign when building header
s = s.replace("""            var title = Glass.Label(PageTitle("petals"), 22, false, true);
            title.Tag = "pagetitle";
            head.Children.Add(title);""",
"""            _pageTitle = Glass.Label(PageTitle("petals"), 22, false, true);
            head.Children.Add(_pageTitle);""")

# 3) replace FindTitle machinery
start = s.index("        TextBlock FindTitle()")
end = s.index("        string PageTitle(string key)")
s = s[:start] + s[end:]

# 4) ShowPage uses the field
s = s.replace("            var titleBlock = FindTitle();\n            if (titleBlock != null) titleBlock.Text = PageTitle(key);",
              "            if (_pageTitle != null) _pageTitle.Text = PageTitle(key);")

# 5) RefreshAccent must also keep _pageTitle valid after rebuild (it does not touch header) — ok

io.open(p, "w", encoding="utf-8").write(s)
print("findtitle fixed")
