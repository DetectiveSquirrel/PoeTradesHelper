using ExileCore;
using System.Windows.Forms;

namespace PoeTradesHelper;

public class MouseClickController
{
    private bool _mouseDown;

    public MouseClickController()
    {
        Input.RegisterKey(Keys.LButton);
    }

    public bool MouseClick { get; private set; }

    public void Update()
    {
        MouseClick = false;
        if (Input.GetKeyState(Keys.LButton))
        {
            _mouseDown = true;
        }
        else
        {
            if (_mouseDown)
            {
                _mouseDown = false;
                MouseClick = true;
            }
        }
    }
}