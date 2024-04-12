﻿using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.AtlasHelper;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using PoeTradesHelper.Chat;
using SharpDX;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PoeTradesHelper;

public class PoeTradesHelperCore : BaseSettingsPlugin<Settings>
{
    private const float ENTRY_HEIGHT = 75;
    private const float EntrySpacing = 3;
    private readonly AreaPlayersController _areaPlayersController = new();
    private readonly MouseClickController _mouseClickController = new();
    private readonly ReplyButtonsController _replyButtonsController = new();
    private AtlasTexture _askInterestingIcon;
    private BannedMessagesFilter _bannedMessagesFilter;
    private CancellationTokenSource _cancellationTokenSource;

    private ChatController _chatController;
    private bool _clipboardTradeProcessorProcessPressed;
    private AtlasTexture _closeTexture;
    private AtlasTexture _entryBgTexture;
    private AtlasTexture _headerTexture;
    private AtlasTexture _iconTrade;
    private AtlasTexture _iconVisitHideout;
    private AtlasTexture _incomeTradeIcon;
    private AtlasTexture _inviteIcon;
    private AtlasTexture _kickIcon;
    private AtlasTexture _leaveIcon;
    private MessagesController _messagesController;
    private string _notificationSound;
    private AtlasTexture _outgoingTradeIcon;
    private string _readeProcessorPrevText;
    private AtlasTexture _repeatIcon;
    private StashTradeController _stashTradeController;
    private TradeLogic _tradeLogic;
    private AtlasTexture _whoIsIcon;

    public override bool Initialise()
    {
        _chatController = new ChatController(this);
        _messagesController = new MessagesController();
        _tradeLogic = new TradeLogic(Settings);
        _stashTradeController = new StashTradeController(this);
        _replyButtonsController.Load(DirectoryFullName);
        _bannedMessagesFilter = new BannedMessagesFilter(Settings);

        _notificationSound = Path.Combine(DirectoryFullName, "Sounds", "notification.wav");
        _iconVisitHideout = GetAtlasTexture("visiteHideout");
        _iconTrade = GetAtlasTexture("trade");
        _headerTexture = GetAtlasTexture("header_bg");
        _entryBgTexture = GetAtlasTexture("entry_bg");
        _closeTexture = GetAtlasTexture("close");
        _incomeTradeIcon = GetAtlasTexture("incoming_arrow");
        _outgoingTradeIcon = GetAtlasTexture("outgoing_arrow");
        _leaveIcon = GetAtlasTexture("leave");
        _kickIcon = GetAtlasTexture("kick");
        _inviteIcon = GetAtlasTexture("invite");
        _askInterestingIcon = GetAtlasTexture("still-interesting");
        _repeatIcon = GetAtlasTexture("reload-history");
        _whoIsIcon = GetAtlasTexture("who-is");

        _chatController.MessageReceived += _messagesController.ReceiveMessage;
        _messagesController.ChatMessageReceived += _bannedMessagesFilter.FilterMessage;
        _bannedMessagesFilter.MessagePassed += _tradeLogic.OnChatMessageReceived;
        _bannedMessagesFilter.MessagePassed += _areaPlayersController.OnChatMessageReceived;

        _tradeLogic.NewTradeReceived += OnNewTradeReceived;
        Settings.DemoMessage.OnPressed += delegate
        {
            _messagesController.ReceiveMessage(
                $"@From {GameController.Player.GetComponent<Player>().PlayerName}: Hi, I would like to buy your Test Item listed for 999 chaos in {GameController.IngameState.ServerData.League}");
        };

        _cancellationTokenSource = new CancellationTokenSource();

        var factory = new TaskFactory(_cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskContinuationOptions.None, TaskScheduler.Default);
        factory.StartNew(UpdateThread, _cancellationTokenSource.Token);

        return base.Initialise();
    }

    private void OnNewTradeReceived()
    {
        if (Settings.PlaySound.Value)
        {
            GameController.SoundController.PlaySound(_notificationSound);
        }
    }

    private void UpdateThread()
    {
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            _chatController.Update();
            Task.Delay(100).Wait();

            //The same functionality as in mercury trade. Press F2, press whisper button on trade site (copy to buffer), unpress F2- it will be printed to chat
            var keyState = Input.GetKeyState(Settings.TradeCopyToChatHotkey.Value);
            if (keyState && !_clipboardTradeProcessorProcessPressed)
            {
                _clipboardTradeProcessorProcessPressed = true;
                _readeProcessorPrevText = ImGui.GetClipboardText();
            }
            else if (!keyState && _clipboardTradeProcessorProcessPressed)
            {
                _clipboardTradeProcessorProcessPressed = false;
                var tradeText = ImGui.GetClipboardText();

                if (_readeProcessorPrevText != tradeText)
                {
                    WinApi.SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
                    Thread.Sleep(30);
                    _chatController.PrintToChat(tradeText);
                }

                _readeProcessorPrevText = null;
            }
        }
    }

    public override void OnPluginDestroyForHotReload()
    {
        _cancellationTokenSource.Cancel();
        base.OnPluginDestroyForHotReload();
    }

    public override void OnClose()
    {
        _cancellationTokenSource.Cancel();
        base.OnClose();
    }

    public override void Render()
    {
        if (_tradeLogic.TradeEntries.Count == 0)
        {
            return;
        }

        _mouseClickController.Update();

        ImGui.SetNextWindowPos(new System.Numerics.Vector2(Settings.PosX, Settings.PosY), ImGuiCond.Once, System.Numerics.Vector2.Zero);

        var windowSize = new System.Numerics.Vector2(Settings.EntryWidth, _tradeLogic.TradeEntries.Count * (ENTRY_HEIGHT + EntrySpacing) + 20);

        ImGui.SetNextWindowSize(windowSize, ImGuiCond.Always);

        var rect = new RectangleF(Settings.PosX, Settings.PosY, windowSize.X, 20);

        var flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoSavedSettings;

        if (!rect.Contains(Input.MousePositionNum))
        {
            flags ^= ImGuiWindowFlags.NoMove;
        }

        var opened = true;

        if (ImGui.Begin($"{Name}", ref opened, flags))
        {
            DrawWindowContent();

            var pos = ImGui.GetWindowPos();
            Settings.PosX = pos.X;
            Settings.PosY = pos.Y;

            var size = ImGui.GetWindowSize();
            Settings.EntryWidth = size.X;
        }

        ImGui.End();
    }

    private void DrawWindowContent()
    {
        var drawPos = new System.Numerics.Vector2(Settings.PosX, Settings.PosY + 20);

        foreach (var tradeEntry in _tradeLogic.TradeEntries)
        {
            var rect = new RectangleF(drawPos.X, drawPos.Y, Settings.EntryWidth, ENTRY_HEIGHT);

            var globalRect = rect;
            const float border = 1;
            globalRect.X -= border;
            globalRect.Y -= border;
            globalRect.Width += border * 2;
            globalRect.Height += border * 2;

            Graphics.DrawImage(_entryBgTexture, globalRect);

            Graphics.DrawBox(globalRect, Settings.TradeMessageBackground);

            var headerRect = rect;
            headerRect.Height = 25;
            DrawHeader(tradeEntry.Value, headerRect);

            var contentRect = rect;
            contentRect.Top += 25;
            DrawContent(tradeEntry.Value, contentRect);

            Graphics.DrawFrame(rect, Settings.TradeEntryBorder.Value, 1);
            drawPos.Y += ENTRY_HEIGHT + EntrySpacing;
        }

        _stashTradeController.Draw(_tradeLogic.TradeEntries.Values);
    }

    private void DrawHeader(TradeEntry tradeEntry, RectangleF headerRect)
    {
        Graphics.DrawImage(_headerTexture, headerRect);

        headerRect.Y += 3;

        if (DrawImageButton(new RectangleF(headerRect.X + 5, headerRect.Y + 1, 18, 18), _whoIsIcon, 2))
        {
            _chatController.PrintToChat($"/whois {tradeEntry.PlayerNick}");
        }

        var inArea = _areaPlayersController.IsPlayerInArea(tradeEntry.PlayerNick);
        var nickPos = new Vector2(headerRect.X + 5 + 20 + 3, headerRect.Y + 1);

        var nickShort = tradeEntry.PlayerNick;

        if (nickShort.Length > 18)
        {
            nickShort = $"{nickShort.Substring(0, 18)}...";
        }

        if (DrawTextButton(ref nickPos,
                18,
                nickShort,
                0,
                inArea
                    ? Color.Green
                    : new Color(255, 211, 78)))
        {
            _chatController.PrintToChat($"@{tradeEntry.PlayerNick} ", false);
        }

        var currencyTextPos = headerRect.TopLeft.Translate(headerRect.Width / 2 - 5);

        var textSize = Graphics.DrawText($"{tradeEntry.CurrencyAmount} {tradeEntry.CurrencyType}", currencyTextPos, Settings.CurrencyColor.Value, FontAlign.Right);

        var rectangleF = new RectangleF(currencyTextPos.X - textSize.X - 5 - 18, currencyTextPos.Y, 18, 18);
        Graphics.DrawImage(tradeEntry.IsIncomingTrade
                ? _outgoingTradeIcon
                : _incomeTradeIcon,
            rectangleF);

        var elapsed = DateTime.Now - tradeEntry.Timestamp;

        Graphics.DrawText(Utils.TimeSpanToString(elapsed), headerRect.TopLeft.Translate(headerRect.Width / 2 + 5), Settings.ElapsedTimeColor.Value);

        const float button_width = 18;
        const float buttons_spacing = 10;

        var buttonsRect = headerRect;
        buttonsRect.X += headerRect.Width - button_width - 3f;
        buttonsRect.Width = button_width;
        buttonsRect.Height = 18;

        if (DrawImageButton(buttonsRect, _closeTexture, 2))
        {
            _tradeLogic.TradeEntries.TryRemove(tradeEntry.UniqueId, out _);
        }

        buttonsRect.X -= button_width + buttons_spacing;

        if (!tradeEntry.IsIncomingTrade)
        {
            if (DrawImageButton(buttonsRect, _leaveIcon, 1))
            {
                _chatController.PrintToChat($"/kick {GameController.Player.GetComponent<Player>().PlayerName}");
                _tradeLogic.TradeEntries.TryRemove(tradeEntry.UniqueId, out _);
            }
        }
        else
        {
            if (DrawImageButton(buttonsRect, _kickIcon, 2))
            {
                _chatController.PrintToChat($"/kick {tradeEntry.PlayerNick}");
                _tradeLogic.TradeEntries.TryRemove(tradeEntry.UniqueId, out _);
            }
        }

        buttonsRect.X -= button_width + buttons_spacing;

        if (DrawImageButton(buttonsRect,
                _iconTrade,
                1,
                inArea
                    ? Color.Yellow
                    : Color.Gray))
        {
            _chatController.PrintToChat($"/tradewith {tradeEntry.PlayerNick}");
        }

        buttonsRect.X -= button_width + buttons_spacing;

        if (DrawImageButton(buttonsRect, _iconVisitHideout))
        {
            _chatController.PrintToChat($"/hideout {tradeEntry.PlayerNick}");
        }

        if (tradeEntry.IsIncomingTrade)
        {
            buttonsRect.X -= button_width + buttons_spacing;

            if (DrawImageButton(buttonsRect, _inviteIcon))
            {
                _chatController.PrintToChat($"/invite {tradeEntry.PlayerNick}");
            }

            buttonsRect.X -= button_width + buttons_spacing + 10;

            if (DrawImageButton(buttonsRect, _closeTexture, color: Color.Red))
            {
                _tradeLogic.TradeEntries.TryRemove(tradeEntry.UniqueId, out _);

                _bannedMessagesFilter.BanMessage(tradeEntry.Message);
            }
        }
    }

    private void DrawContent(TradeEntry tradeEntry, RectangleF contentRect)
    {
        var nameText = tradeEntry.ItemName;

        if (tradeEntry.ItemAmount != "")
        {
            nameText = tradeEntry.ItemAmount + " " + tradeEntry.ItemName;
        }

        Graphics.DrawText(nameText, contentRect.TopLeft.Translate(30, 2), Color.Yellow);
        Graphics.DrawText(tradeEntry.OfferText, contentRect.TopLeft.Translate(contentRect.Width - 30, 2), Color.Red, FontAlign.Right);

        var repeatButtonRect = contentRect;
        repeatButtonRect.Y += 2;
        repeatButtonRect.X += repeatButtonRect.Width - 21;
        repeatButtonRect.Width = 18;
        repeatButtonRect.Height = 18;

        if (tradeEntry.IsIncomingTrade)
        {
            if (DrawImageButton(repeatButtonRect, _askInterestingIcon, 2))
            {
                _chatController.PrintToChat($"@{tradeEntry.PlayerNick} Let me know if you still interesting in {tradeEntry.ItemName} for {tradeEntry.CurrencyAmount} {tradeEntry.CurrencyType}");
            }
        }
        else
        {
            if (DrawImageButton(repeatButtonRect, _repeatIcon, 2))
            {
                _chatController.PrintToChat($"@{tradeEntry.PlayerNick} {tradeEntry.Message}");
            }
        }

        var buttonsDrawPos = contentRect.TopLeft;
        buttonsDrawPos.Y += 25;
        buttonsDrawPos.X += 5;

        var buttons = tradeEntry.IsIncomingTrade
            ? _replyButtonsController.IncomingReplies
            : _replyButtonsController.OutgoingReplies;

        foreach (var replyButtonInfo in buttons)
        {
            if (DrawTextButton(ref buttonsDrawPos, 20, replyButtonInfo.ButtonName))
            {
                _chatController.PrintToChat($"@{tradeEntry.PlayerNick} {replyButtonInfo.Message}");

                if (replyButtonInfo.GoToOwnHideout)
                {
                    _tradeLogic.TradeEntries.TryRemove(tradeEntry.UniqueId, out _);

                    _chatController.PrintToChat($"/kick {GameController.Player.GetComponent<Player>().PlayerName}");
                    _chatController.PrintToChat("/hideout");
                }
                else if (replyButtonInfo.KickLeaveParty)
                {
                    _tradeLogic.TradeEntries.TryRemove(tradeEntry.UniqueId, out _);

                    if (tradeEntry.IsIncomingTrade)
                    {
                        _chatController.PrintToChat($"/kick {tradeEntry.PlayerNick}");
                    }
                    else
                    {
                        _chatController.PrintToChat($"/kick {GameController.Player.GetComponent<Player>().PlayerName}");
                    }
                }
                else if (replyButtonInfo.Close)
                {
                    _tradeLogic.TradeEntries.TryRemove(tradeEntry.UniqueId, out _);
                }
            }
        }
    }

    #region Overrides of BaseSettingsPlugin<Settings>

    public override void OnUnload()
    {
        base.OnUnload();
        _cancellationTokenSource.Cancel();
    }

    public override void EntityAddedAny(Entity entity)
    {
        if (entity.Type != EntityType.Player)
        {
            return;
        }

        if (entity.Address == GameController.Player.Address)
        {
            return;
        }

        var player = entity.GetComponent<Player>();

        if (string.IsNullOrEmpty(player.PlayerName))
        {
            return;
        }

        _areaPlayersController.RegisterPlayerInArea(player.PlayerName);
    }

    public override void EntityRemoved(Entity entity)
    {
        if (entity.Type != EntityType.Player)
        {
            return;
        }

        if (entity.Address == GameController.Player.Address)
        {
            return;
        }

        var player = entity.GetComponent<Player>();

        if (player == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(player.PlayerName))
        {
            return;
        }

        _areaPlayersController.UnregisterPlayerInArea(player.PlayerName);
    }

    #endregion

    #region DrawUtils

    private bool DrawImageButton(RectangleF rect, AtlasTexture texture, float imageMargin = 0, Color? color = null)
    {
        var result = DrawButtonBase(rect);

        if (imageMargin != 0)
        {
            rect.X += imageMargin;
            rect.Y += imageMargin;
            rect.Width -= imageMargin * 2;
            rect.Height -= imageMargin * 2;
        }

        Graphics.DrawImage(texture, rect, color ?? Color.White);

        return result;
    }

    private bool DrawTextButton(ref Vector2 pos, float height, string text, float sideMargin = 5, Color? color = null)
    {
        var textSize = Graphics.MeasureText(text);

        var rect = new RectangleF(pos.X, pos.Y, textSize.X + sideMargin * 2, height);
        pos.X += textSize.X + sideMargin * 2 + 5;

        Graphics.DrawText(text, rect.Center, color ?? Color.White, FontAlign.Center | FontAlign.VerticalCenter);

        return DrawButtonBase(rect);
    }

    private bool DrawButtonBase(RectangleF rect)
    {
        var bgColor = Settings.ButtonBorder.Value;
        var contains = rect.Contains(Input.MousePosition);

        if (contains)
        {
            bgColor = new Color(198, 193, 154);
        }

        Graphics.DrawFrame(rect, bgColor, 1);

        if (contains && _mouseClickController.MouseClick)
        {
            return true;
        }

        return false;
    }

    #endregion
}