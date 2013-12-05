using _4charm.ViewModels;
using HtmlAgilityPack;
using Microsoft.Phone.Tasks;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace _4charm.Models
{
    /// <summary>
    /// The "Blocks" property of a RichTextBox is not a DependencyProperty and can't be bound.
    /// Instead we use an attached property which just proxies into that property for us.
    /// 
    /// This class also does the actual HTML document translation into XAML run elements for
    /// use in the RichTextBox, but this translation should actually be done in the ViewModel
    /// to improve scrolling performance.
    /// </summary>
    public static class BindableTextBlock
    {
        public static string GetFormattedText(DependencyObject obj)
        {
            return (string)obj.GetValue(FormattedTextProperty);
        }

        public static void SetFormattedText(DependencyObject obj, HtmlDocument value)
        {
            obj.SetValue(FormattedTextProperty, value);
        }

        public static readonly DependencyProperty FormattedTextProperty =
            DependencyProperty.RegisterAttached("FormattedText",
            typeof(HtmlDocument),
            typeof(BindableTextBlock),
            new PropertyMetadata(null, FormattedTextChanged));

        /// <summary>
        /// This matches quote links within the current thread.
        /// </summary>
        private static Regex r = new Regex("^(/([^/]+)/res/)?(\\d+)#p(\\d+)$");

        /// <summary>
        /// Quote linking to a text board. Currently this just removes the link information since
        /// we don't support text boards.
        /// </summary>
        private static Regex r2 = new Regex("//dis\\.4chan\\.org/([^/]+)/");

        /// <summary>
        /// Link to an different thread, either on the current board or another image board. We translate
        /// these into links which will load the correct board first.
        /// </summary>
        private static Regex r3 = new Regex("//boards\\.4chan\\.org/([^/]+)/|/([^/]+)/");

        /// <summary>
        /// The bound HtmlDocument changed. Map the HTML nodes into XAML nodes and build the corresponding
        /// RichTextBox for display.
        /// </summary>
        private static void FormattedTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            // TODO, Split up long boxes so that they are under height 2000px
            RichTextBox textBlock = sender as RichTextBox;
            textBlock.Blocks.Clear();

            HtmlDocument doc = e.NewValue as HtmlDocument;
            Paragraph para = new Paragraph();
            foreach (var node in doc.DocumentNode.ChildNodes)
            {
                HtmlNode _node = node;
                // Hack to allow nested font tag
                if (_node.Name == "font" && _node.FirstChild != null) _node = node.FirstChild;
                // Hack to allow strange quotes (nested in a span class quote tag)
                else if (_node.Name == "span" && _node.Attributes.Contains("class") &&
                    _node.Attributes["class"].Value == "quote" && _node.HasChildNodes
                    && _node.FirstChild.Name == "a" && _node.FirstChild.Attributes.Contains("class") &&
                    _node.FirstChild.Attributes["class"].Value == "quotelink" &&
                    _node.FirstChild.Attributes.Contains("href")) _node = _node.FirstChild;

                if (_node.Name == "br") para.Inlines.Add(new LineBreak());
                // Regular quote
                else if (_node.Name == "a" && _node.Attributes.Contains("class") &&
                    _node.Attributes["class"].Value == "quotelink" &&
                    _node.Attributes.Contains("href"))
                {
                    Match m = r.Match(_node.Attributes["href"].Value);
                    // Thread quote
                    if (m.Success)
                    {
                        Hyperlink h = new Hyperlink();
                        h.Click += (hsender, he) =>
                        {
                            if (textBlock.DataContext is PostViewModel)
                            {
                                // Because this click is *not* a routed event, we can't cancel it or
                                // mark it as handled and it will actually bubble down to the child post
                                // grid and hit that tap event too. To work around this we set the rootframe
                                // hit test property as a sentinel value to signal that tap handler to ignore
                                // the invocation.
                                App.IsPostTapAllowed = false;
                                PostViewModel pvm = (PostViewModel)textBlock.DataContext;
                                pvm.QuoteLinkTapped(m.Groups[2].Value, ulong.Parse(m.Groups[3].Value), ulong.Parse(m.Groups[4].Value));

                                // We clear this sentinel property on the dispatcher, since the routed tap events actually get queued on
                                // the dispatcher and don't happen until this current function returns.
                                Deployment.Current.Dispatcher.BeginInvoke(() => App.IsPostTapAllowed = true);
                            }
                        };
                        h.Inlines.Add(new Run()
                        {
                            Foreground = App.Current.Resources["LinkBrush"] as SolidColorBrush,
                            Text = WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ",")
                        });
                        para.Inlines.Add(h);
                    }
                    else
                    {
                        
                        Match m3 = r3.Match(_node.Attributes["href"].Value);
                        if (m3.Success)
                        {
                            Hyperlink h = new Hyperlink();
                            h.Click += (hsender, he) =>
                            {
                                if (textBlock.DataContext is PostViewModel)
                                {
                                    App.IsPostTapAllowed = false;
                                    (textBlock.DataContext as PostViewModel).BoardLinkTapped(m3.Groups[1].Value + m3.Groups[2].Value);
                                    Deployment.Current.Dispatcher.BeginInvoke(() => App.IsPostTapAllowed = true);
                                }
                            };
                            h.Inlines.Add(new Run()
                            {
                                Foreground = App.Current.Resources["LinkBrush"] as SolidColorBrush,
                                Text = WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ",")
                            });
                            para.Inlines.Add(h);
                        }
                        else
                        {
                            Match m2 = r2.Match(_node.Attributes["href"].Value);
                            // Text board quote?
                            if (m2.Success)
                            {
                                para.Inlines.Add(WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ","));
                            }
                            else
                            {
                                //Debug.WriteLine(_node.OuterHtml);
                                para.Inlines.Add(WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ","));
                            }
                        }
                    }
                }
                // Dead Quote
                else if (_node.Name == "span" &&
                    _node.Attributes.Contains("class") &&
                    _node.Attributes["class"].Value == "quote deadlink")
                {
                    para.Inlines.Add(new Run()
                    {
                        Foreground = App.Current.Resources["GreentextBrush"] as SolidColorBrush,
                        Text = WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ",")
                    });
                }
                // Greentext
                else if (_node.Name == "span" && _node.Attributes.Contains("class") &&
                    _node.Attributes["class"].Value == "quote")
                {
                    para.Inlines.Add(new Run()
                    {
                        Foreground = App.Current.Resources["GreentextBrush"] as SolidColorBrush,
                        Text = WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ",")
                    });
                }
                // Dead Quote 2#
                else if (_node.Name == "span" && _node.Attributes.Contains("class") && _node.Attributes["class"].Value == "quote deadlink")
                {
                    para.Inlines.Add(new Run()
                    {
                        Foreground = App.Current.Resources["GreentextBrush"] as SolidColorBrush,
                        Text = WebUtility.HtmlDecode(_node.ChildNodes[0].InnerText).Replace("&#039;", "'").Replace("&#44;", ",")
                    });
                }
                // Banned text
                else if ((_node.Name == "b" || _node.Name == "strong") && _node.Attributes.Contains("style") &&
                    _node.Attributes["style"].Value.Replace(" ", "") == "color:red;")
                {
                    para.Inlines.Add(new Run()
                    {
                        Foreground = App.Current.Resources["BannedBrush"] as SolidColorBrush,
                        Text = WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ",")
                    });
                }
                // Spoiler
                else if (_node.Name == "s")
                {
                    Hyperlink h = new Hyperlink()
                    {
                        TextDecorations = null
                    };
                    int len = WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ",").Length;
                    // To simulate spoiler display, we just put the unicode wide block symbol over and over in a spoiler color,
                    // since we can't actually highlight a region of the text or obscure it normally.
                    Run r = new Run()
                    {
                        Foreground = App.Current.Resources["SpoilerBrush"] as SolidColorBrush,
                        Text = new String('\u2588', len),
                        FontSize = 17
                    };
                    bool isClicked = false;
                    h.Click += (hsender, he) =>
                    {
                        if (isClicked) return;

                        isClicked = true;
                        App.IsPostTapAllowed = false;
                        r.Text = WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ",");
                        r.FontSize = 17;
                        h.TextDecorations = null;
                        Deployment.Current.Dispatcher.BeginInvoke(() => App.IsPostTapAllowed = true);
                    };
                    h.Inlines.Add(r);
                    para.Inlines.Add(h);
                }
                //Hyperlink
                else if (_node.Name == "a" && _node.Attributes.Contains("href"))
                {
                    Hyperlink h = new Hyperlink() { NavigateUri = new Uri(_node.Attributes["href"].Value), TargetName = "_blank" };
                    h.Click += (hsender, he) =>
                    {
                        App.IsPostTapAllowed = false;
                        Deployment.Current.Dispatcher.BeginInvoke(() => App.IsPostTapAllowed = true);
                    };
                    h.Inlines.Add(new Run()
                    {
                        Foreground = App.Current.Resources["LinkBrush"] as SolidColorBrush,
                        Text = WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ",")
                    });
                    para.Inlines.Add(h);
                }
                // Bold
                else if (_node.Name == "b" || _node.Name == "strong")
                {
                    Bold b = new Bold();
                    b.Inlines.Add(WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ","));
                    para.Inlines.Add(b);
                }
                // Underline
                else if (_node.Name == "u")
                {
                    Underline u = new Underline();
                    u.Inlines.Add(WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ","));
                    para.Inlines.Add(u);

                }
                else if (_node.Name == "small")
                {
                    para.Inlines.Add(new Run() { Text = WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ","), FontSize = 14 });
                }
                // Regular text
                else if (_node.Name == "#text")
                {
                    // para.Inlines.Add(WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ","));
                    List<Inline> inlines = MarkupLinks(WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ","));
                    foreach (Inline inline in inlines) para.Inlines.Add(inline);
                }
                else
                {
                    //Debug.WriteLine(_node.OuterHtml);
                    para.Inlines.Add(WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ","));
                }
            }

            textBlock.Blocks.Add(para);
        }

        /// <summary>
        /// This is a really naive linkifier for strings of text, that just wraps things which look like links
        /// (start with "http" or "www") in hyperlink elements to be tappable.
        /// </summary>
        /// <param name="text"></param>
        /// <returns>The marked up runs with hyperlinks inserted as appropriate.</returns>
        private static List<Inline> MarkupLinks(string text)
        {
            List<Inline> inlines = new List<Inline>();

            int filledIndex = 0;
            int index = 0;
            while (true)
            {
                int httpIndex = text.IndexOf("http", index);
                int wwwIndex = text.IndexOf("www", index);

                bool isWWW = false;
                int minIndex = httpIndex;
                if ((wwwIndex >= 0 && wwwIndex < httpIndex) || minIndex < 0)
                {
                    isWWW = true;
                    minIndex = wwwIndex;
                }
                if (minIndex < 0)
                {
                    inlines.Add(new Run() { Text = text.Substring(filledIndex, text.Length - filledIndex) });
                    break;
                }

                int nextSpaceIndex = text.IndexOf(' ', minIndex);
                if (nextSpaceIndex < 0) nextSpaceIndex = text.Length;
                string link = (isWWW ? "http://" : "") + text.Substring(minIndex, nextSpaceIndex - minIndex);

                Uri uri;
                if (Uri.TryCreate(link, UriKind.Absolute, out uri))
                {
                    Hyperlink h = new Hyperlink();
                    h.Inlines.Add(new Run()
                    {
                        Text = text.Substring(minIndex, nextSpaceIndex - minIndex),
                        Foreground = App.Current.Resources["LinkBrush"] as SolidColorBrush
                    });
                    h.Click += (sender, e) =>
                    {
                        App.IsPostTapAllowed = false;
                        new WebBrowserTask() { Uri = uri }.Show();
                        Deployment.Current.Dispatcher.BeginInvoke(() => App.IsPostTapAllowed = true);
                    };
                    inlines.Add(new Run() { Text = text.Substring(filledIndex, minIndex - filledIndex) });
                    inlines.Add(h);

                    filledIndex = nextSpaceIndex;
                    index = nextSpaceIndex;
                }
                else
                {
                    index = minIndex + 1;
                }
            }

            return inlines;
        }
    }
}
