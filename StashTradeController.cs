﻿using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using StashItemsDict = System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<int>>;

namespace PoeTradesHelper;

public class StashTradeController
{
    private readonly PoeTradesHelperCore _main;

    private Dictionary<string, StashItemsDict> _itemPrices = new();

    public StashTradeController(PoeTradesHelperCore main)
    {
        _main = main;
    }

    public void Draw(ICollection<TradeEntry> entries)
    {
        HighlightTradeItems(entries);
        //UpdateStashTradeItems();
    }

    private void HighlightTradeItems(ICollection<TradeEntry> entries)
    {
        var stashElement = _main.GameController.Game.IngameState.IngameUi.StashElement;

        if (!stashElement.IsVisible)
        {
            return;
        }

        var viewAllStashPanel = stashElement.ViewAllStashPanel;
        var yShift = _main.GameController.Memory.Read<float>(viewAllStashPanel.Address + 0x5C);
        var stashNames = stashElement.AllStashNames;
        var currentStash = stashElement.IndexVisibleStash;
        var visibleStash = stashElement.VisibleStash;

        foreach (var tradeEntry in entries)
        {
            if (!tradeEntry.IsIncomingTrade)
            {
                continue;
            }

            if (tradeEntry.ItemPosInfo == null) //try draw without pos
            {
                var items = stashElement.VisibleStash?.VisibleInventoryItems;

                if (items != null)
                {
                    var tradeItems = items.Where(x => GetItemName(x.Item) == tradeEntry.ItemName);

                    foreach (var item in tradeItems)
                    {
                        _main.Graphics.DrawFrame(item.GetClientRect(), Color.Magenta, 2);
                    }
                }

                continue;
            }

            var index = stashNames.IndexOf(tradeEntry.ItemPosInfo.TabName);

            if (index != currentStash)
            {
                if (viewAllStashPanel.IsVisible && index != -1)
                {
                    var childAtIndex = stashElement.ViewAllStashPanelChildren[index];

                    if (childAtIndex != null)
                    {
                        var rect = childAtIndex.GetClientRect();
                        rect.Y += yShift * viewAllStashPanel.Scale;
                        _main.Graphics.DrawFrame(rect, Color.Yellow, 2);
                    }
                    else
                    {
                        _main.LogError($"TradeController: No child at {index}");
                    }
                }
            }
            else if (visibleStash != null)
            {
                var cellCount = visibleStash.InvType == InventoryType.QuadStash
                    ? 24
                    : 12;

                var visibleStashRect = visibleStash.GetClientRectCache;
                var cellSize = visibleStashRect.Width / cellCount;

                var itemRect = new RectangleF(visibleStashRect.X + (tradeEntry.ItemPosInfo.Pos.X - 1) * cellSize,
                    visibleStashRect.Y + (tradeEntry.ItemPosInfo.Pos.Y - 1) * cellSize,
                    cellSize,
                    cellSize);

                _main.Graphics.DrawFrame(itemRect, Color.Yellow, 3);
            }
        }
    }

    private void UpdateStashTradeItems()
    {
        var stashElement = _main.GameController.Game.IngameState.IngameUi.StashElement;

        if (!stashElement.IsVisible)
        {
            return;
        }

        var items = stashElement.VisibleStash?.VisibleInventoryItems;

        if (items == null)
        {
            return;
        }

        var priceDict = new StashItemsDict();

        foreach (var normalInventoryItem in items)
        {
            try
            {
                var item = normalInventoryItem.Item;
                if (item == null)
                {
                    continue;
                }

                var baseComp = item.GetComponent<Base>();
                if (string.IsNullOrEmpty(baseComp.PublicPrice))
                {
                    continue;
                }

                var itemName = GetItemName(item);

                if (priceDict.TryGetValue(itemName, out var list))
                {
                }
            }
            catch (Exception e)
            {
            }
        }
    }

    private string GetItemName(Entity entity)
    {
        var name = string.Empty;

        var mods = entity.GetComponent<Mods>();
        if (mods != null)
        {
            name += $"{mods.UniqueName} ";
        }

        var baseComp = entity.GetComponent<Base>();

        if (baseComp != null)
        {
            name += baseComp.Name;
        }

        return name;
    }

    //public List<int> GetItemAmount(string itemName)
    //{

    //}
}