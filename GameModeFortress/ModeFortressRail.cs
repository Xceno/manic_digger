﻿using System;
using System.Collections.Generic;
using System.Text;
using ManicDigger;
using OpenTK;
using ManicDigger.Collisions;
using ManicDigger.Renderers;

namespace ManicDigger
{
    public partial class ManicDiggerGameWindow
    {
        [Inject]
        public RailMapUtil d_RailMapUtil { get; set; }
        [Inject]
        public MinecartRenderer d_MinecartRenderer { get; set; }
        void RailOnNewFrame(float dt)
        {
            LocalPlayerAnimationHint.InVehicle = railriding;
            LocalPlayerAnimationHint.DrawFix = railriding ? new Vector3(0, -0.7f, 0) : new Vector3();

            bool turnright = keyboardstate[GetKey(OpenTK.Input.Key.D)];
            bool turnleft = keyboardstate[GetKey(OpenTK.Input.Key.A)];
            RailSound();
            if (railriding)
            {
                ENABLE_FREEMOVE = true;
                ENABLE_MOVE = false;
                LocalPlayerPosition = CurrentRailPos();
                currentrailblockprogress += currentvehiclespeed * (float)dt;
                if (currentrailblockprogress >= 1)
                {
                    lastdirection = currentdirection;
                    currentrailblockprogress = 0;
                    var newenter = new TileEnterData();
                    newenter.BlockPosition = NextTile(currentdirection, currentrailblock);
                    //slope
                    if (GetUpDownMove(currentrailblock,
                        DirectionUtils.ResultEnter(DirectionUtils.ResultExit(currentdirection))) == UpDown.Up)
                    {
                        newenter.BlockPosition.Z++;
                    }
                    if (GetUpDownMove(newenter.BlockPosition + new Vector3(0, 0, -1),
                        DirectionUtils.ResultEnter(DirectionUtils.ResultExit(currentdirection))) == UpDown.Down)
                    {
                        newenter.BlockPosition.Z--;
                    }

                    newenter.EnterDirection = DirectionUtils.ResultEnter(DirectionUtils.ResultExit(currentdirection));
                    var newdir = BestNewDirection(PossibleRails(newenter), turnleft, turnright);
                    if (newdir == null)
                    {
                        //end of rail
                        currentdirection = DirectionUtils.Reverse(currentdirection);
                    }
                    else
                    {
                        currentdirection = newdir.Value;
                        currentrailblock = newenter.BlockPosition;
                    }
                }
            }
            if (keyboardstate[GetKey(OpenTK.Input.Key.W)] && GuiTyping != TypingState.Typing)
            {
                currentvehiclespeed += 1f * (float)dt;
            }
            if (keyboardstate[GetKey(OpenTK.Input.Key.S)] && GuiTyping != TypingState.Typing)
            {
                currentvehiclespeed -= 5f * (float)dt;
            }
            if (currentvehiclespeed < 0)
            {
                currentvehiclespeed = 0;
            }
            //todo fix
            //if (viewport.keypressed != null && viewport.keypressed.Key == OpenTK.Input.Key.Q)            
            if (!wasqpressed && keyboardstate[GetKey(OpenTK.Input.Key.Q)] && GuiTyping != TypingState.Typing)
            {
                Reverse();
            }

            if (!wasepressed && keyboardstate[GetKey(OpenTK.Input.Key.E)] && !railriding && !ENABLE_FREEMOVE && GuiTyping != TypingState.Typing)
            {
                currentrailblock = new Vector3((int)LocalPlayerPosition.X,
                    (int)LocalPlayerPosition.Z, (int)LocalPlayerPosition.Y - 1);
                if (!MapUtil.IsValidPos(d_Map, (int)currentrailblock.X, (int)currentrailblock.Y, (int)currentrailblock.Z))
                {
                    ExitVehicle();
                }
                else
                {
                    var railunderplayer = d_Data.Rail[d_Map.GetBlock((int)currentrailblock.X, (int)currentrailblock.Y, (int)currentrailblock.Z)];
                    railriding = true;
                    CharacterHeight = minecartheight;
                    currentvehiclespeed = 0;
                    if ((railunderplayer & RailDirectionFlags.Horizontal) != 0)
                    {
                        currentdirection = VehicleDirection12.HorizontalRight;
                    }
                    else if ((railunderplayer & RailDirectionFlags.Vertical) != 0)
                    {
                        currentdirection = VehicleDirection12.VerticalUp;
                    }
                    else if ((railunderplayer & RailDirectionFlags.UpLeft) != 0)
                    {
                        currentdirection = VehicleDirection12.UpLeftUp;
                    }
                    else if ((railunderplayer & RailDirectionFlags.UpRight) != 0)
                    {
                        currentdirection = VehicleDirection12.UpRightUp;
                    }
                    else if ((railunderplayer & RailDirectionFlags.DownLeft) != 0)
                    {
                        currentdirection = VehicleDirection12.DownLeftLeft;
                    }
                    else if ((railunderplayer & RailDirectionFlags.DownRight) != 0)
                    {
                        currentdirection = VehicleDirection12.DownRightRight;
                    }
                    else
                    {
                        ExitVehicle();
                    }
                    lastdirection = currentdirection;
                }
            }
            else if (!wasepressed && keyboardstate[GetKey(OpenTK.Input.Key.E)] && railriding && GuiTyping != TypingState.Typing)
            {
                ExitVehicle();
                LocalPlayerPosition += new Vector3(0, 0.7f, 0);
            }
            wasqpressed = keyboardstate[GetKey(OpenTK.Input.Key.Q)] && GuiTyping != TypingState.Typing;
            wasepressed = keyboardstate[GetKey(OpenTK.Input.Key.E)] && GuiTyping != TypingState.Typing;
        }
        private void ExitVehicle()
        {
            CharacterHeight = WalkCharacterHeight;
            railriding = false;
            ENABLE_FREEMOVE = false;
            ENABLE_MOVE = true;
        }
        private void Reverse()
        {
            currentdirection = DirectionUtils.Reverse(currentdirection);
            currentrailblockprogress = 1 - currentrailblockprogress;
            lastdirection = currentdirection;
            //currentvehiclespeed = 0;
        }
        DateTime lastrailsoundtime;
        int lastrailsound;
        private void RailSound()
        {
            float railsoundpersecond = currentvehiclespeed;
            if (railsoundpersecond > 10)
            {
                railsoundpersecond = 10;
            }
            d_Audio.PlayAudioLoop("railnoise.wav", railriding && railsoundpersecond > 0.1f);
            if (!railriding)
            {
                return;
            }
            if ((DateTime.Now - lastrailsoundtime).TotalSeconds > 1 / railsoundpersecond)
            {
                d_Audio.Play("rail" + (lastrailsound + 1) + ".wav");
                lastrailsoundtime = DateTime.Now;
                lastrailsound++;
                if (lastrailsound >= 4)
                {
                    lastrailsound = 0;
                }
            }
        }
        private float WalkCharacterHeight = 1.5f;
        float currentvehiclespeed;
        Vector3 currentrailblock;
        float currentrailblockprogress = 0;
        VehicleDirection12 currentdirection;
        VehicleDirection12 lastdirection;
        Vector3 CurrentRailPos()
        {
            var slope = d_RailMapUtil.GetRailSlope((int)currentrailblock.X,
                (int)currentrailblock.Y, (int)currentrailblock.Z);
            Vector3 a = currentrailblock;
            float x_correction = 0;
            float y_correction = 0;
            float z_correction = 0;
            switch (currentdirection)
            {
                case VehicleDirection12.HorizontalRight:
                    x_correction += currentrailblockprogress;
                    y_correction += 0.5f;
                    if (slope == RailSlope.TwoRightRaised)
                        z_correction += currentrailblockprogress;
                    if (slope == RailSlope.TwoLeftRaised)
                        z_correction += 1 - currentrailblockprogress;
                    break;
                case VehicleDirection12.HorizontalLeft:
                    x_correction += 1.0f - currentrailblockprogress;
                    y_correction += 0.5f;
                    if (slope == RailSlope.TwoRightRaised)
                        z_correction += 1 - currentrailblockprogress;
                    if (slope == RailSlope.TwoLeftRaised)
                        z_correction += currentrailblockprogress;
                    break;
                case VehicleDirection12.VerticalDown:
                    x_correction += 0.5f;
                    y_correction += currentrailblockprogress;
                    if (slope == RailSlope.TwoDownRaised)
                        z_correction += currentrailblockprogress;
                    if (slope == RailSlope.TwoUpRaised)
                        z_correction += 1 - currentrailblockprogress;
                    break;
                case VehicleDirection12.VerticalUp:
                    x_correction += 0.5f;
                    y_correction += 1.0f - currentrailblockprogress;
                    if (slope == RailSlope.TwoDownRaised)
                        z_correction += 1 - currentrailblockprogress;
                    if (slope == RailSlope.TwoUpRaised)
                        z_correction += currentrailblockprogress;
                    break;
                case VehicleDirection12.UpLeftLeft:
                    x_correction += 0.5f * (1.0f - currentrailblockprogress);
                    y_correction += 0.5f * currentrailblockprogress;
                    break;
                case VehicleDirection12.UpLeftUp:
                    x_correction += 0.5f * currentrailblockprogress;
                    y_correction += 0.5f - 0.5f * currentrailblockprogress;
                    break;
                case VehicleDirection12.UpRightRight:
                    x_correction += 0.5f + 0.5f * currentrailblockprogress;
                    y_correction += 0.5f * currentrailblockprogress;
                    break;
                case VehicleDirection12.UpRightUp:
                    x_correction += 1.0f - 0.5f * currentrailblockprogress;
                    y_correction += 0.5f - 0.5f * currentrailblockprogress;
                    break;
                case VehicleDirection12.DownLeftLeft:
                    x_correction += 0.5f * (1 - currentrailblockprogress);
                    y_correction += 1.0f - 0.5f * currentrailblockprogress;
                    break;
                case VehicleDirection12.DownLeftDown:
                    x_correction += 0.5f * currentrailblockprogress;
                    y_correction += 0.5f + 0.5f * currentrailblockprogress;
                    break;
                case VehicleDirection12.DownRightRight:
                    x_correction += 0.5f + 0.5f * currentrailblockprogress;
                    y_correction += 1.0f - 0.5f * currentrailblockprogress;
                    break;
                case VehicleDirection12.DownRightDown:
                    x_correction += 1.0f - 0.5f * currentrailblockprogress;
                    y_correction += 0.5f + 0.5f * currentrailblockprogress;
                    break;
            }
            //+1 because player can't be inside rail block (picking wouldn't work)
            return new Vector3(a.X + x_correction, a.Z + railheight + 1 + z_correction, a.Y + y_correction);
        }
        float railheight = 0.3f;
        private float minecartheight { get { return 1.5f - 1; } }
        public struct TileEnterData
        {
            public Vector3 BlockPosition;
            public TileEnterDirection EnterDirection;
        }
        public VehicleDirection12Flags PossibleRails(TileEnterData enter)
        {
            Vector3 new_position = enter.BlockPosition;
            VehicleDirection12Flags possible_rails = VehicleDirection12Flags.None;
            if (MapUtil.IsValidPos(d_Map, (int)enter.BlockPosition.X, (int)enter.BlockPosition.Y, (int)enter.BlockPosition.Z))
            {
                RailDirectionFlags newpositionrail = d_Data.Rail[
                    d_Map.GetBlock((int)enter.BlockPosition.X, (int)enter.BlockPosition.Y, (int)enter.BlockPosition.Z)];
                List<VehicleDirection12> all_possible_rails = new List<VehicleDirection12>();
                foreach (var z in DirectionUtils.PossibleNewRails(enter.EnterDirection))
                {
                    if ((newpositionrail & DirectionUtils.ToRailDirectionFlags(DirectionUtils.ToRailDirection(z)))
                        != RailDirectionFlags.None)
                    {
                        all_possible_rails.Add(z);
                    }
                }
                possible_rails = DirectionUtils.ToVehicleDirection12Flags(all_possible_rails);
            }
            return possible_rails;
        }
        public static Vector3 NextTile(VehicleDirection12 direction, Vector3 currentTile)
        {
            return NextTile(DirectionUtils.ResultExit(direction), currentTile);
        }
        public static Vector3 NextTile(TileExitDirection direction, Vector3 currentTile)
        {
            switch (direction)
            {
                case TileExitDirection.Left:
                    return new Vector3(currentTile.X - 1, currentTile.Y, currentTile.Z);
                case TileExitDirection.Right:
                    return new Vector3(currentTile.X + 1, currentTile.Y, currentTile.Z);
                case TileExitDirection.Up:
                    return new Vector3(currentTile.X, currentTile.Y - 1, currentTile.Z);
                case TileExitDirection.Down:
                    return new Vector3(currentTile.X, currentTile.Y + 1, currentTile.Z);
                default:
                    throw new ArgumentException("direction");
            }
        }
        bool railriding = false;
        bool wasqpressed = false;
        bool wasepressed = false;
        VehicleDirection12? BestNewDirection(VehicleDirection12Flags dir, bool turnleft, bool turnright)
        {
            if (turnright)
            {
                if ((dir & VehicleDirection12Flags.DownRightRight) != 0)
                {
                    return VehicleDirection12.DownRightRight;
                }
                if ((dir & VehicleDirection12Flags.UpRightUp) != 0)
                {
                    return VehicleDirection12.UpRightUp;
                }
                if ((dir & VehicleDirection12Flags.UpLeftLeft) != 0)
                {
                    return VehicleDirection12.UpLeftLeft;
                }
                if ((dir & VehicleDirection12Flags.DownLeftDown) != 0)
                {
                    return VehicleDirection12.DownLeftDown;
                }
            }
            if (turnleft)
            {
                if ((dir & VehicleDirection12Flags.DownRightDown) != 0)
                {
                    return VehicleDirection12.DownRightDown;
                }
                if ((dir & VehicleDirection12Flags.UpRightRight) != 0)
                {
                    return VehicleDirection12.UpRightRight;
                }
                if ((dir & VehicleDirection12Flags.UpLeftUp) != 0)
                {
                    return VehicleDirection12.UpLeftUp;
                }
                if ((dir & VehicleDirection12Flags.DownLeftLeft) != 0)
                {
                    return VehicleDirection12.DownLeftLeft;
                }
            }
            foreach (var v in DirectionUtils.ToVehicleDirection12s(dir))
            {
                return v;
            }
            return null;
        }
        enum UpDown
        {
            None,
            Up,
            Down,
        }
        UpDown GetUpDownMove(Vector3 railblock, TileEnterDirection dir)
        {
            if (!MapUtil.IsValidPos(d_Map, (int)railblock.X, (int)railblock.Y, (int)railblock.Z))
            {
                return UpDown.None;
            }
            //going up
            RailSlope slope = d_RailMapUtil.GetRailSlope((int)railblock.X, (int)railblock.Y, (int)railblock.Z);
            if (slope == RailSlope.TwoDownRaised && dir == TileEnterDirection.Up)
            {
                return UpDown.Up;
            }
            if (slope == RailSlope.TwoUpRaised && dir == TileEnterDirection.Down)
            {
                return UpDown.Up;
            }
            if (slope == RailSlope.TwoLeftRaised && dir == TileEnterDirection.Right)
            {
                return UpDown.Up;
            }
            if (slope == RailSlope.TwoRightRaised && dir == TileEnterDirection.Left)
            {
                return UpDown.Up;
            }
            //going down
            if (slope == RailSlope.TwoDownRaised && dir == TileEnterDirection.Down)
            {
                return UpDown.Down;
            }
            if (slope == RailSlope.TwoUpRaised && dir == TileEnterDirection.Up)
            {
                return UpDown.Down;
            }
            if (slope == RailSlope.TwoLeftRaised && dir == TileEnterDirection.Left)
            {
                return UpDown.Down;
            }
            if (slope == RailSlope.TwoRightRaised && dir == TileEnterDirection.Right)
            {
                return UpDown.Down;
            }
            return UpDown.None;
        }
        public IEnumerable<IModelToDraw> Models
        {
            get
            {
                if (railriding)
                {
                    var m = new Minecart();
                    m.renderer = d_MinecartRenderer;
                    m.position = LocalPlayerPosition;
                    m.direction = currentdirection;
                    m.lastdirection = lastdirection;
                    m.progress = currentrailblockprogress;
                    yield return m;
                }
            }
        }
    }
}
