using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;
using System.Windows.Forms;

namespace PoeTradesHelper;

public class Settings : ISettings
{
    public ButtonNode DemoMessage { get; set; } = new();
    public float PosX { get; set; } = 100;
    public float PosY { get; set; } = 100;
    public float EntryWidth { get; set; } = 300;

    //public ColorNode TradeEntryBg { get; set; } = new ColorNode(new Color(42, 44, 43));
    //public ColorNode TradeEntryFg { get; set; } = new ColorNode(new Color(52, 62, 61));
    public ToggleNode LockPosition { get; set; } = new(true);
    public ColorNode TradeEntryBorder { get; set; } = new(new Color(62, 73, 72));
    public ColorNode TradeMessageBackground { get; set; } = new(Color.Black);

    //public ColorNode NickColor { get; set; } = new ColorNode(new Color(255, 211, 78));
    public ColorNode ElapsedTimeColor { get; set; } = new(new Color(255, 211, 78));
    public ColorNode CurrencyColor { get; set; } = new(new Color(255, 250, 250));
    public ColorNode ButtonBg { get; set; } = new(new Color(70, 80, 79));
    public ColorNode ButtonBorder { get; set; } = new(new Color(96, 106, 105));
    public ToggleNode RemoveDuplicatedTrades { get; set; } = new(true);
    public ToggleNode PlaySound { get; set; } = new(true);
    public RangeNode<int> ChatScanDelay { get; set; } = new(1000, 10, 10000);
    public RangeNode<int> BanMessageTimeMinutes { get; set; } = new(20, 1, 100);
    public HotkeyNode TradeCopyToChatHotkey { get; } = new(Keys.F2);
    public ToggleNode Enable { get; set; } = new(true);
}