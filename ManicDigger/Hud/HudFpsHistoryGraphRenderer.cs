﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ManicDigger.Renderers
{
    public interface IDraw2d
    {
        void Draw2dTexture(int textureid, float x1, float y1, float width, float height, int? inAtlasId, Color color);
        int WhiteTexture();
        void Draw2dText(string text, float x, float y, float fontsize, Color? color);
        void Draw2dTextures(Draw2dData[] todraw, int textureid);
    }
    public class HudFpsHistoryGraphRenderer
    {
        [Inject]
        public IViewportSize d_ViewportSize;
        [Inject]
        public IDraw2d d_Draw;
        List<float> m_fpshistory = new List<float>();
        List<float> fpshistory
        {
            get
            {
                while (m_fpshistory.Count < MAX_COUNT - 1)
                {
                    m_fpshistory.Add(0);
                }
                return m_fpshistory;
            }
        }
        public int MAX_COUNT = 300;
        Draw2dData[] todraw;
        public void DrawFpsHistoryGraph()
        {
            float maxtime = 0;
            foreach (var v in fpshistory)
            {
                if (v > maxtime)
                {
                    maxtime = v;
                }
            }
            float historyheight = 80;
            int posx = 25;
            int posy = d_ViewportSize.Height - (int)historyheight - 20;
            FastColor[] colors = new[] { new FastColor(Color.Black), new FastColor(Color.Red) };
            Color linecolor = Color.White;

            if (todraw == null)
            {
                todraw = new Draw2dData[MAX_COUNT];
            }
            for (int i = 0; i < fpshistory.Count; i++)
            {
                float time = fpshistory[i];
                time = (time * 60) * historyheight;
                FastColor c = Interpolation.InterpolateColor((float)i / fpshistory.Count, colors);
                todraw[i].x1 = posx + i;
                todraw[i].y1 = posy - time;
                todraw[i].width = 1;
                todraw[i].height = time;
                todraw[i].inAtlasId = null;
                todraw[i].color = c;
            }
            d_Draw.Draw2dTextures(todraw, d_Draw.WhiteTexture());

            d_Draw.Draw2dTexture(d_Draw.WhiteTexture(), posx, posy - historyheight, fpshistory.Count, 1, null, linecolor);
            d_Draw.Draw2dTexture(d_Draw.WhiteTexture(), posx, posy - historyheight * (60f / 75), fpshistory.Count, 1, null, linecolor);
            d_Draw.Draw2dTexture(d_Draw.WhiteTexture(), posx, posy - historyheight * (60f / 30), fpshistory.Count, 1, null, linecolor);
            d_Draw.Draw2dTexture(d_Draw.WhiteTexture(), posx, posy - historyheight * (60f / 150), fpshistory.Count, 1, null, linecolor);
            d_Draw.Draw2dText("60", posx, posy - historyheight * (60f / 60), 6, null);
            d_Draw.Draw2dText("75", posx, posy - historyheight * (60f / 75), 6, null);
            d_Draw.Draw2dText("30", posx, posy - historyheight * (60f / 30), 6, null);
            d_Draw.Draw2dText("150", posx, posy - historyheight * (60f / 150), 6, null);
        }
        public void Update(float dt)
        {
            fpshistory.RemoveAt(0);
            fpshistory.Add(dt);
        }
    }
}
