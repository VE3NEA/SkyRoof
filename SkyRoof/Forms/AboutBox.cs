using System.Diagnostics;
using System.Reflection;
using VE3NEA;

namespace SkyRoof
{
  public partial class AboutBox : Form
  {
    public AboutBox()
    {
      InitializeComponent();
      WebsiteLabel.LinkColor = Theme.Link;
      Text = $"About {Utils.GetAppName()}";
      label1.Text = Utils.GetVersionString();
      label2.Text = Utils.GetCopyrightString();
    }

    private void WebsiteLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
      WebsiteLabel.LinkVisited = true;
      Process.Start(new ProcessStartInfo("https://ve3nea.github.io/SkyRoof") { UseShellExecute = true });
    }
  }
}
