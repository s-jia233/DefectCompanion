using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DefectCompanion
{
    public sealed partial class PetView
    {
        private string hoveredPower;
        private static readonly Dictionary<int,Geometry> badgeShapes=new Dictionary<int,Geometry>();
        private static Rect PowerRect(int index,int count)
        { return new Rect(210-count*31/2.0+index*31,360,26,26); }
        public PowerStatus PowerAt(Point point)
        {
            if(point.Y<358 || point.Y>388 || point.X<45 || point.X>375) return null;
            var powers=owner.Engine.PowerStatuses();
            for(int i=0;i<powers.Count;i++) if(PowerRect(i,powers.Count).Contains(point)) return powers[i];
            return null;
        }
        public void UpdatePowerHover(Point point)
        {
            PowerStatus power=PowerAt(point); string key=power==null?null:power.Key;
            if(key==hoveredPower) return;
            hoveredPower=key; ToolTip=power==null?null:power.Tooltip;
            ToolTipService.SetInitialShowDelay(this,180);ToolTipService.SetShowDuration(this,12000);
        }
        public string PowerSummary
        {
            get { var labels=new List<string>();foreach(var p in owner.Engine.PowerStatuses()) labels.Add(p.Name+" "+p.Amount); return String.Join("、",labels.ToArray()); }
        }
        private void DrawPowerBar(DrawingContext dc)
        {
            var powers=owner.Engine.PowerStatuses();
            for(int i=0;i<powers.Count;i++)
            {
                PowerStatus power=powers[i];Rect rect=PowerRect(i,powers.Count);
                if(power.Key==hoveredPower)
                    dc.DrawRoundedRectangle(Brush(Color.FromRgb(21,41,56),.82),new Pen(Brush(Color.FromRgb(148,222,244),.7),1),new Rect(rect.X-2,rect.Y-2,30,30),5,5);
                dc.DrawImage(GameArt.Bitmap("power-"+power.Key),rect);
                Geometry shape;
                if(!badgeShapes.TryGetValue(power.Amount,out shape))
                {
                    var text=new FormattedText(power.Amount.ToString(),CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface(new FontFamily("Microsoft YaHei UI"),FontStyles.Normal,FontWeights.Bold,FontStretches.Normal),12,Brushes.White,1);
                    shape=text.BuildGeometry(new Point(0,0));shape.Freeze();badgeShapes[power.Amount]=shape;
                }
                dc.PushTransform(new TranslateTransform(rect.Right-(power.Amount>=10?14:8),rect.Bottom-13));
                dc.DrawGeometry(Brushes.White,new Pen(Brush(Color.FromRgb(9,17,25),1),2.8),shape);dc.Pop();
            }
        }
    }
}
