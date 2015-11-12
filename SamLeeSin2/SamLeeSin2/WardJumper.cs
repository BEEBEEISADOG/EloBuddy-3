﻿using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;

namespace SamLeeSin2
{
    class WardJumper
    {
        //WardJump
        private static long _lastCheck;
        public static long LastWard;
        private static Vector3 _jumpPos;
        public static bool WardjumpActive;
        public static Menu WardjumpMenu;
        private static AIHeroClient _Player
        {
            get { return ObjectManager.Player; }
        }

        public static void Init()
        {
            Drawing.OnDraw += Drawing_OnDraw;

            WardjumpMenu = Program.menu.AddSubMenu("WardJump Settings", "wardJumpSettings");
            WardjumpMenu.AddGroupLabel("Wardjump Settings");
            var a = WardjumpMenu.Add("alwaysMax", new CheckBox("Always Jump To Max Range"));
            var b = WardjumpMenu.Add("onlyToCursor", new CheckBox("Always Jump To Cursor", false));
            a.OnValueChange += delegate { if (a.CurrentValue) b.CurrentValue = false; };
            b.OnValueChange += delegate { if (b.CurrentValue) a.CurrentValue = false; };
            WardjumpMenu.AddSeparator();
            WardjumpMenu.AddLabel("Time Modifications");
            WardjumpMenu.Add("checkTime", new Slider("Position Reset Time (ms)", 100, 1, 2000));
            WardjumpMenu.AddSeparator();
            WardjumpMenu.AddLabel("Keybind Settings");

            var wj = WardjumpMenu.Add("wardjumpKeybind",
                new KeyBind("WardJump", false, KeyBind.BindTypes.HoldActive, 'T'));

            WardjumpMenu.Add("drawWJ", new CheckBox("Draw WardJump"));

            GameObject.OnCreate += GameObject_OnCreate;
            Game.OnTick += delegate
            {
                if (wj.CurrentValue)
                {
                    WardjumpActive = true;
                    WardJump(Game.CursorPos, a.CurrentValue, b.CurrentValue);
                    return;
                }
                WardjumpActive = false;
            };
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.IsAlly && sender is Obj_AI_Base && sender.Name.ToLower().Contains("ward") && sender.Distance(_Player) < 600 && _jumpPos.Distance(sender) < 200 && WardjumpActive)
            {
                Program.W.Cast((Obj_AI_Base)sender);
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (WardjumpActive && WardjumpMenu["drawWJ"].Cast<CheckBox>().CurrentValue)
            {
                Circle.Draw(Color.Teal, 100, _jumpPos);
                Circle.Draw(Color.White, 600, _Player.Position);
            }
        }

        public static void WardJump(Vector3 pos, bool max, bool cursorOnly)
        {
            Orbwalker.OrbwalkTo(Game.CursorPos.Extend(Game.CursorPos, 200).To3D());
            if (!_jumpPos.IsValid() || _lastCheck <= Environment.TickCount)
            {
                _jumpPos = pos;
                _lastCheck = Environment.TickCount + WardjumpMenu["checkTime"].Cast<Slider>().CurrentValue;
            }

            var jumpPoint = _jumpPos;
            if (max && jumpPoint.Distance(_Player.Position) > 600)
            {
                jumpPoint = _Player.Position.Extend(_jumpPos, 600).To3D();
            }
            else if (cursorOnly && jumpPoint.Distance(_Player.Position) > 600)
            {
                return;
            }

            _jumpPos = jumpPoint;
            var ward =
                ObjectManager.Get<Obj_AI_Base>()
                    .FirstOrDefault(a => a.IsAlly && a.Distance(_jumpPos) < 100);
            if (ward != null)
            {
                if (Program.W.IsReady() && Program.W.Name == Program.Spells["W1"] &&
                    Program.LastCast[Program.Spells["W1"]] + 500 < Environment.TickCount)
                {
                    Player.CastSpell(SpellSlot.W, ward);
                    Program.LastCast[Program.Spells["W1"]] = Environment.TickCount;
                }
            }
            else
            {
                var wardSpot = GetWardSlot();
                if (wardSpot == null)
                {
                    return;
                }
                if (Program.W.IsReady() && Program.W.Name == Program.Spells["W1"] &&
                    Program.LastCast[Program.Spells["W1"]] + 500 < Environment.TickCount &&
                    LastWard + 400 < Environment.TickCount)
                {
                    GetWardSlot().Cast(_jumpPos);
                    LastWard = Environment.TickCount;
                }
            }
        }

        public static InventorySlot GetWardSlot()
        {
            var wardIds = new[] { ItemId.Warding_Totem_Trinket, ItemId.Greater_Stealth_Totem_Trinket, ItemId.Greater_Vision_Totem_Trinket, ItemId.Sightstone, ItemId.Ruby_Sightstone, ItemId.Stealth_Ward, ItemId.Vision_Ward, ItemId.Farsight_Orb_Trinket };
            return _Player.InventoryItems.FirstOrDefault(a => wardIds.Contains(a.Id) && a.IsWard && a.CanUseItem());
        }
    }
}
