using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API.Core;
using KitsuneMenu.Core;
using KitsuneMenu.Core.Enums;
using KitsuneMenu.Core.MenuItems;
using KitsuneMenuMenu = KitsuneMenu.Core.Menu;
using Menu.Enums;

namespace Menu.Enums
{
    public enum MenuButtons : ulong
    {
        None = 0,
        Select = 1,
        Back = 2,
        Up = 3,
        Down = 4,
        Left = 5,
        Right = 6,
        Exit = 7,
        Input = 8,
    }

    public enum MenuItemType
    {
        Text,
        Choice,
        Bool,
        ChoiceBool,
        Button,
        Spacer,
        Slider,
        Input
    }
}

namespace Menu
{
    public class MenuValue
    {
        public MenuValue(string value)
        {
            Value = value;
        }

        public string Value { get; set; }

        private string _prefix = string.Empty;
        private string _suffix = string.Empty;

        public string? OriginalPrefix { get; private set; }
        public string? OriginalSuffix { get; private set; }

        public string Prefix
        {
            get => _prefix;
            set
            {
                OriginalPrefix ??= value;
                _prefix = value;
            }
        }

        public string Suffix
        {
            get => _suffix;
            set
            {
                OriginalSuffix ??= value;
                _suffix = value;
            }
        }

        public override string ToString() => $"{Prefix}{Value}{Suffix}";

        public virtual MenuValue Copy() => new(Value) { Prefix = Prefix, Suffix = Suffix };
    }

    public class MenuButtonCallback : MenuValue
    {
        private const int MaxLength = 26;

        public MenuButtonCallback(string value, string data, Action<CCSPlayerController, string> callback, bool disabled = false, bool trimValue = false)
            : base(trimValue ? TrimValue(value) : value)
        {
            Callback = callback;
            Data = data;
            Disabled = disabled;
        }

        public Action<CCSPlayerController, string> Callback { get; }
        public string Data { get; }
        public bool Disabled { get; }

        private static string TrimValue(string value) => value.Length > MaxLength ? value[..MaxLength] + "..." : value;

        public override MenuValue Copy() => new MenuButtonCallback(Value, Data, Callback, Disabled, true) { Prefix = Prefix, Suffix = Suffix };
    }

    public class MenuItem
    {
        public MenuItemType Type { get; init; }
        public bool Pinwheel { get; init; }
        public MenuValue? Head { get; set; }
        public MenuValue? Tail { get; set; }
        public List<MenuValue>? Values { get; set; }
        public int Option { get; set; }
        public int[] Data { get; set; }
        public string DataString { get; set; } = string.Empty;

        public MenuItem(MenuItemType type, MenuValue head, List<MenuValue> values, MenuValue tail, bool pinwheel = false)
        {
            Type = type;
            Pinwheel = pinwheel;
            Head = head;
            Tail = tail;
            Values = values;
            Data = new int[values.Count];
        }

        public MenuItem(MenuItemType type, MenuValue head, List<MenuValue> values, bool pinwheel = false)
        {
            Type = type;
            Pinwheel = pinwheel;
            Head = head;
            Values = values;
            Data = new int[values.Count];
        }

        public MenuItem(MenuItemType type, List<MenuValue> values, MenuValue tail, bool pinwheel = false)
        {
            Type = type;
            Pinwheel = pinwheel;
            Tail = tail;
            Values = values;
            Data = new int[values.Count];
        }

        public MenuItem(MenuItemType type, List<MenuValue> values, bool pinwheel = false)
        {
            Type = type;
            Pinwheel = pinwheel;
            Values = values;
            Data = new int[values.Count];
        }

        public MenuItem(MenuItemType type, MenuValue head, MenuValue tail)
        {
            Type = type;
            Head = head;
            Tail = tail;
            Data = new int[1];
        }

        public MenuItem(MenuItemType type, MenuValue head)
        {
            Type = type;
            Head = head;
            Data = new int[1];
        }

        public MenuItem(MenuItemType type)
        {
            Type = type;
            Data = new int[1];
        }
    }

    public class MenuBase
    {
        public MenuBase(List<MenuItem> items)
        {
            Items = items;
        }

        public int Option { get; set; }
        public List<MenuItem> Items { get; }
    }

    public class KitsuneMenu : IDisposable
    {
        private bool _disposed;

        public KitsuneMenu(BasePlugin plugin)
        {
            global::KitsuneMenu.KitsuneMenu.Init();
        }

        public void ShowScrollableMenu(
            CCSPlayerController controller,
            string title,
            List<MenuItem> items,
            Action<MenuButtons, MenuBase, MenuItem?>? callback,
            bool isSubmenu = false,
            bool freezePlayer = false,
            int visibleItems = 5,
            Dictionary<int, object>? defaultValues = null,
            bool disableDeveloper = false)
        {
            if (controller == null || !controller.IsValid || items.Count == 0)
                return;

            var builder = global::KitsuneMenu.KitsuneMenu.Create(title);
            builder.MaxVisibleItems(Math.Max(1, visibleItems));

            if (freezePlayer)
                builder.ForceFreeze();
            else
                builder.NoFreeze();

            KitsuneMenuMenu? parent = null;
            if (isSubmenu && global::KitsuneMenu.KitsuneMenu.TryGetSession(controller, out var session))
            {
                parent = session.CurrentMenu as KitsuneMenuMenu;
            }

            var menuBase = new MenuBase(items);

            for (int index = 0; index < items.Count; index++)
            {
                var item = items[index];

                switch (item.Type)
                {
                    case MenuItemType.Spacer:
                        builder.AddSeparator();
                        continue;

                    case MenuItemType.Text:
                        builder.AddText(RenderText(item), TextAlign.Left);
                        continue;

                    case MenuItemType.Bool:
                        {
                            bool defaultValue = ResolveBoolDefault(defaultValues, index);
                            item.Data[0] = defaultValue ? 1 : 0;
                            var label = RenderText(item);
                            builder.AddToggle(label, defaultValue, (player, value) =>
                            {
                                item.Data[0] = value ? 1 : 0;
                                menuBase.Option = index;
                                callback?.Invoke(MenuButtons.Select, menuBase, item);
                            });
                            break;
                        }

                    default:
                        {
                            var label = RenderText(item);
                            bool disabled = item.Values?.FirstOrDefault() is MenuButtonCallback btn && btn.Disabled;

                            builder.AddButton(label, player =>
                            {
                                menuBase.Option = index;
                                if (item.Values?.FirstOrDefault() is MenuButtonCallback buttonCallback && callback == null)
                                {
                                    buttonCallback.Callback?.Invoke(player, buttonCallback.Data);
                                    return;
                                }

                                callback?.Invoke(MenuButtons.Select, menuBase, item);
                            });

                            if (disabled)
                            {
                                builder.EnabledWhen(_ => false);
                            }
                            break;
                        }
                }
            }

            var menu = builder.Build();
            menu.Parent = parent;
            menu.Show(controller);
        }

        public void ClearMenus(CCSPlayerController controller)
        {
            global::KitsuneMenu.KitsuneMenu.CloseMenu(controller);
        }

        public void Dispose()
        {
            if (_disposed) return;
            global::KitsuneMenu.KitsuneMenu.Cleanup();
            _disposed = true;
        }

        private static bool ResolveBoolDefault(Dictionary<int, object>? defaults, int index)
        {
            if (defaults != null && defaults.TryGetValue(index, out var value))
            {
                return value switch
                {
                    bool b => b,
                    int i => i != 0,
                    string s when bool.TryParse(s, out var parsed) => parsed,
                    _ => false
                };
            }

            return false;
        }

        private static string RenderText(MenuItem item)
        {
            if (item.Head != null && item.Values != null)
            {
                var valuesText = string.Join("", item.Values.Select(v => v.ToString()));
                return $"{item.Head}{valuesText}{item.Tail}";
            }

            if (item.Values != null && item.Values.Count > 0)
            {
                return string.Join("", item.Values.Select(v => v.ToString()));
            }

            if (item.Head != null)
            {
                return item.Head.ToString();
            }

            return string.Empty;
        }
    }
}
