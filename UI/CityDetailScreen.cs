using BarelyUI;
using BarelyUI.Layouts;
using Industry.Simulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industry.UI
{
    public class CityDetailScreen : VerticalLayout
    {
        
        public CityDetailScreen(CitiesScreen citiesScreen)
        {


            Layout.PushLayout("bottomButtons");
            HorizontalLayout bottomButtons = new HorizontalLayout();
            bottomButtons.SetAnchor(AnchorX.Left, AnchorY.Bottom);
            Button back = new Button("back");
            back.OnMouseClick += this.Close;
            back.OnMouseClick += citiesScreen.Open;

            Button close = new Button("close");
            close.OnMouseClick = this.Close;
            bottomButtons.AddChild(back, close);

            AddChild(bottomButtons);
            Layout.PopLayout("bottomButtons");


        }

        public void Open(City city)
        {
            Open();



        }

    }
}
