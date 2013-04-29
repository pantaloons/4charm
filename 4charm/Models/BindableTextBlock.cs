using _4charm.ViewModels;
using HtmlAgilityPack;
using System;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace _4charm.Models
{
    public static class BindableTextBlock
    {
        public static string GetFormattedText(DependencyObject obj)
        {
            return (string)obj.GetValue(FormattedTextProperty);
        }

        public static void SetFormattedText(DependencyObject obj, string value)
        {
            obj.SetValue(FormattedTextProperty, value);
        }

        public static readonly DependencyProperty FormattedTextProperty =
            DependencyProperty.RegisterAttached("FormattedText",
            typeof(string),
            typeof(BindableTextBlock),
            new PropertyMetadata("", FormattedTextChanged));

        private static Regex r = new Regex("^(/([^/]+)/res/)?(\\d+)#p(\\d+)$");
        private static Regex r2 = new Regex("//dis\\.4chan\\.org/([^/]+)/");
        private static Regex r3 = new Regex("//boards\\.4chan\\.org/([^/]+)/|/([^/]+)/");
        private static void FormattedTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            //TODO, Split up long boxes so that they are under height 2000px
            RichTextBox textBlock = sender as RichTextBox;
            textBlock.Blocks.Clear();

            string value = e.NewValue as string;
            if (value == null) return;

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(value);
            Paragraph para = new Paragraph();
            foreach (var node in doc.DocumentNode.ChildNodes)
            {
                HtmlNode _node = node;
                //Hack to allow nested font tag
                if (_node.Name == "font" && _node.FirstChild != null) _node = node.FirstChild;
                //Hack to allow strange quotes (nested in a span class quote tag)
                else if (_node.Name == "span" && _node.Attributes.Contains("class") &&
                    _node.Attributes["class"].Value == "quote" && _node.HasChildNodes
                    && _node.FirstChild.Name == "a" && _node.FirstChild.Attributes.Contains("class") &&
                    _node.FirstChild.Attributes["class"].Value == "quotelink" &&
                    _node.FirstChild.Attributes.Contains("href")) _node = _node.FirstChild;

                if (_node.Name == "br") para.Inlines.Add(new LineBreak());
                //Regular quote
                else if (_node.Name == "a" && _node.Attributes.Contains("class") &&
                    _node.Attributes["class"].Value == "quotelink" &&
                    _node.Attributes.Contains("href"))
                {
                    Match m = r.Match(_node.Attributes["href"].Value);
                    //Thread quote
                    if (m.Success)
                    {
                        Hyperlink h = new Hyperlink();
                        h.Click += (hsender, he) =>
                        {
                            if (textBlock.DataContext is PostViewModel)
                            {
                                App.RootFrame.IsHitTestVisible = false;
                                PostViewModel pvm = (PostViewModel)textBlock.DataContext;
                                pvm.QuoteLinkTapped(m.Groups[2].Value, ulong.Parse(m.Groups[3].Value), ulong.Parse(m.Groups[4].Value));
                                Deployment.Current.Dispatcher.BeginInvoke(() => App.RootFrame.IsHitTestVisible = true);
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
                                    App.RootFrame.IsHitTestVisible = false;
                                    (textBlock.DataContext as PostViewModel).BoardLinkTapped(m3.Groups[1].Value + m3.Groups[2].Value);
                                    Deployment.Current.Dispatcher.BeginInvoke(() => App.RootFrame.IsHitTestVisible = true);
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
                            //Text board quote?
                            if (m2.Success)
                            {
                                para.Inlines.Add(WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ","));
                            }
                            else
                            {
                                Debug.WriteLine(_node.OuterHtml);
                                para.Inlines.Add(WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ","));
                            }
                        }
                    }
                }
                //Dead Quote
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
                //Greentext
                else if (_node.Name == "span" && _node.Attributes.Contains("class") &&
                    _node.Attributes["class"].Value == "quote")
                {
                    para.Inlines.Add(new Run()
                    {
                        Foreground = App.Current.Resources["GreentextBrush"] as SolidColorBrush,
                        Text = WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ",")
                    });
                }
                //Dead Quote 2#
                else if (_node.Name == "span" && _node.Attributes.Contains("class") && _node.Attributes["class"].Value == "quote deadlink")
                {
                    para.Inlines.Add(new Run()
                    {
                        Foreground = App.Current.Resources["GreentextBrush"] as SolidColorBrush,
                        Text = WebUtility.HtmlDecode(_node.ChildNodes[0].InnerText).Replace("&#039;", "'").Replace("&#44;", ",")
                    });
                }
                //Banned text
                else if ((_node.Name == "b" || _node.Name == "strong") && _node.Attributes.Contains("style") &&
                    _node.Attributes["style"].Value.Replace(" ", "") == "color:red;")
                {
                    para.Inlines.Add(new Run()
                    {
                        Foreground = App.Current.Resources["BannedBrush"] as SolidColorBrush,
                        Text = WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ",")
                    });
                }
                //Spoiler
                else if (_node.Name == "s")
                {
                    Hyperlink h = new Hyperlink()
                    {
                        TextDecorations = null
                    };
                    int len = WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ",").Length;
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
                        App.RootFrame.IsHitTestVisible = false;
                        r.Text = WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ",");
                        r.FontSize = 17;
                        h.TextDecorations = null;
                        Deployment.Current.Dispatcher.BeginInvoke(() => App.RootFrame.IsHitTestVisible = true);
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
                        App.RootFrame.IsHitTestVisible = false;
                        Deployment.Current.Dispatcher.BeginInvoke(() => App.RootFrame.IsHitTestVisible = true);
                    };
                    h.Inlines.Add(new Run()
                    {
                        Foreground = App.Current.Resources["LinkBrush"] as SolidColorBrush,
                        Text = WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ",")
                    });
                    para.Inlines.Add(h);
                }
                //Bold
                else if (_node.Name == "b" || _node.Name == "strong")
                {
                    Bold b = new Bold();
                    b.Inlines.Add(WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ","));
                    para.Inlines.Add(b);
                }
                //Underline
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
                //Regular text
                else if (_node.Name == "#text")
                {
                    para.Inlines.Add(WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ","));
                }
                else
                {
                    Debug.WriteLine(_node.OuterHtml);
                    para.Inlines.Add(WebUtility.HtmlDecode(_node.InnerText).Replace("&#039;", "'").Replace("&#44;", ","));
                }
            }
            textBlock.Blocks.Add(para);
        }
    }
}
