﻿using BaseLib.Media.Display;
using BaseLib.Media.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xwt;

namespace BaseLib.Media.OpenTK
{
    public interface IXwtRender //: BaseLib.Xwt.IXwt
    {
        void CreateForWidgetContext(IRenderer renderer, IRenderOwner rendererimpl, Canvas widget);
        void FreeWindowInfo(Widget widget);
        //void GetInfo(Widget widget, out object win);
        //  void MakeCurrent(Widget widget);
        //   void EndScene(Widget widget);
        // void LoadAll(Widget widget);
        void StartRender(IRenderer renderer, Widget widget);
        void EndRender(IRenderer renderer, Widget widget);
        void SwapBuffers(Widget widget);
        // void Render(Widget widget, IRenderOwner renderer);
        /*     void StartScene(Widget win, GraphicsContext ctx, int width, int height);
void EndScene(Widget win, GraphicsContext ctx);*/
    }
    public interface IRenderOwner
    {
        bool preparerender(IRenderFrame destination, long time, bool dowait);
        void render(IRenderFrame destination, long time, rectangle r);
        void StartRender(IRenderer renderer);
        void EndRender(IRenderer renderer);
        /*     void SkipRender(long ticks);*/

        void DoEvents(Func<bool> cancelfunc);
    }
}
