using ExileCore;
using ExileCore.Shared;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace PoeTradesHelper.Chat;

public class ChatController
{
    //private const string LOG_PATH =
    //    @"C:\HomeProjects\Games\_PoE\HUD\PoEHelper\Plugins\Compiled\PoeTradesHelper\chatLog.txt";

    private readonly GameController _gameController;
    private readonly Settings _settings;
    private readonly Stopwatch _updateSw = Stopwatch.StartNew();
    private long _lastMessageAddress;

    public ChatController(PoeTradesHelperCore main)
    {
        _gameController = main.GameController;
        _settings = main.Settings;
        //File.Delete(LOG_PATH);
        ScanChat(true);
    }

    public event Action<string> MessageReceived = delegate { };

    public void Update()
    {
        if (_updateSw.ElapsedMilliseconds > _settings.ChatScanDelay.Value)
        {
            _updateSw.Restart();
            ScanChat(false);
        }
    }

    private void ScanChat(bool firstScan)
    {
        var messageElements = _gameController.Game.IngameState.IngameUi.ChatBox.MessageElements.ToList();

        var msgQueue = new Queue<string>();
        for (var i = messageElements.Count - 1; i >= 0; i--)
        {
            var messageElement = messageElements[i];

            if (messageElement.Address == _lastMessageAddress)
            {
                break;
            }

            if (!messageElement.IsVisibleLocal)
            {
                continue;
            }

            msgQueue.Enqueue(messageElements[i].TextNoTags);
        }

        _lastMessageAddress = messageElements.LastOrDefault()?.Address ?? 0;

        if (firstScan)
            return;

        while (msgQueue.Count > 0)
        {
            try
            {
                MessageReceived(msgQueue.Dequeue());
            }
            catch (Exception e)
            {
                DebugWindow.LogError($"Error processing chat message. Error: {e.Message}", 5);
            }
        }
    }

    public void PrintToChat(string message, bool send = true)
    {
        if (!_gameController.Window.IsForeground())
        {
            WinApi.SetForegroundWindow(_gameController.Window.Process.MainWindowHandle);
        }

        //TODO: Check that chat is opened or no
        SendKeys.SendWait("{ENTER}");
        ImGui.SetClipboardText(message);
        SendKeys.SendWait("^v");

        if (send)
        {
            SendKeys.SendWait("{ENTER}");
        }
        //WinApi.SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
        //WinApi.SetForegroundWindow(_gameController.Window.Process.MainWindowHandle);
    }
}