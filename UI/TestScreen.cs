using Barely.Util;
using BarelyUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industry.UI
{
    public class TestScreen : VerticalLayout
    {

        int selected = 0;
        VerticalLayout[] buildings;

        Sprite frameSpr;
        KeyValueText selectedText;
        Text buildingName;
        KeyValueText[] buildingStats;
        Button finishButton;

        public TestScreen(Sprite houseSpr, Sprite frameSpr)
        {
            this.frameSpr = frameSpr;
            Padding = new Point(10, 10);
            Margin = 10;
            SetFixedSize(700, 500);

            HorizontalLayout mainHor = new HorizontalLayout();
            mainHor.SetLayoutSizeForBoth(LayoutSize.MatchParent);

            VerticalLayout leftOverview = new VerticalLayout();
            leftOverview.SetFixedSize(200, 300).SetLayoutSize(LayoutSize.FixedSize, LayoutSize.MatchParent);
            leftOverview.AddScrollbar();
            leftOverview.Margin = 6;
            leftOverview.Padding = new Point(6,2);

            int buildingsCount = 12;

            buildings = new VerticalLayout[buildingsCount];

            for (int i = 0; i < buildingsCount; i++)
            {
                int index = i;
                VerticalLayout inner = new VerticalLayout();
                inner.OnMouseEnter = () => { MouseOvered(index); };
                buildings[i] = inner;
                inner.Padding = new Point(0, 4);
                inner.SetLayoutSize(LayoutSize.MatchParent, LayoutSize.WrapContent);
                Image im = new Image(houseSpr);                
                inner.OnMouseClick = () => { SelectBuilding(index); };
                inner.AddChild(im);
                inner.AddChild(new Text($"Name {i}").SetAllignments(Allignment.Middle, Allignment.Middle));
                leftOverview.AddChild(inner);
            }

            selectedText = new KeyValueText("Selected Buildings:", selected.ToString());
            selectedText.layoutSizeX = LayoutSize.WrapContent;

            VerticalLayout specifics = new VerticalLayout();
            specifics.overwriteChildLayout = false;
            specifics.SetMargin(6);
            specifics.SetLayoutSizeForBoth(LayoutSize.MatchParent);
            //specifics.SetChildAllignment(Allignment.Bottom);
            specifics.childAllignX = AnchorX.Right;

            VerticalLayout buildingInfos = new VerticalLayout();
            buildingInfos.Padding = new Point(10);
            //buildingInfos.GetStandardSprite(true);
            buildingInfos.SetLayoutSize(LayoutSize.MatchParent, LayoutSize.WrapContent);
            buildingName = new Text("Building Name");
            buildingInfos.AddChild(buildingName);
            buildingStats = new KeyValueText[] { new KeyValueText("building info Test", "12"),
                                                 new KeyValueText("and more", "13"),
                                                 new KeyValueText("even more", "14"),
                                                 new KeyValueText("even more", "15"),
                                                 new KeyValueText("even more", "15")};

            foreach(var el in buildingStats)
            {
                buildingInfos.AddChild(el);
            }

            specifics.AddChild(selectedText);
            specifics.AddChild(buildingInfos);

            mainHor.AddChild(leftOverview);
            mainHor.AddChild(specifics);

            HorizontalLayout buttons = new HorizontalLayout();
            //buttons.SetChildAllignment(Allignment.Right).SetMargin(10);
            buttons.SetLayoutSize(LayoutSize.MatchParent, LayoutSize.WrapContent);

            finishButton = new Button("Finish Selection");            
            buttons.AddChild(finishButton);
            buttons.AddChild(new Button("Cancel"));

            specifics.AddChild(buttons);

            AddChild(mainHor);

        }

        private void MouseOvered(int index)
        {
            buildingName.SetText($"Name {index}");
        }
        
        private void SelectBuilding(int index)
        {
            VerticalLayout l = buildings[index];

            if (l.sprite == null)
            {
                if(selected < 4)
                {
                    selected++;
                    l.sprite = frameSpr;
                }
            }
            else
            {
                selected--;
                l.sprite = null;            
            }

            selectedText.SetValue(selected.ToString());
        }
       
        public override void SetSizeAndPosition(Canvas canvas, Point position, Point size)
        {
            base.SetSizeAndPosition(canvas, position, size);
        }

    }
}
