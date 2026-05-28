using System.Drawing;
using System.Windows.Forms;

namespace VaR;

public class MyRenderer : ToolStripProfessionalRenderer
{
    protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item is ToolStripButton { CheckOnClick: true, Checked: true })
        {
            Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);
            e.Graphics.FillRectangle(Brushes.Black, bounds);
        }
        else base.OnRenderButtonBackground(e);
    }
}