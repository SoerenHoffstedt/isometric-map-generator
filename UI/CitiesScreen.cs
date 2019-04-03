using BarelyUI;
using Industry.Simulation;
using Industry.World;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industry.UI
{
    public class CitiesScreen : VerticalLayout
    {
        List<City> cities;

        public CitiesScreen(Point size, List<City> cities) : base()
        {
            this.cities = cities;

            this.SetFixedSize(size);
            childLayoutOverwrite = LayoutSize.MatchParent;
            childAllignY = AnchorY.Middle;            

            foreach (City c in cities)
            {
                CityEntry entry = new CityEntry(c);
                AddChild(entry);
            }

        }

        public override void Open()
        {
            base.Open();
            foreach(UIElement e in childElements)
            {
                var ce = (CityEntry)e;
                ce.UpdateTexts();
            }
            
        }

    }

    class CityEntry : HorizontalLayout
    {
        private City city;


        Text name;
        KeyValueText lvl1;
        KeyValueText lvl2;
        KeyValueText lvl3;
        Button buttonDetail;


        public CityEntry(City city)
        {
            Padding = Point.Zero;
            this.city = city;
            name = new Text("aaaaaaaaaaaaaaaaaaa");
            lvl1 = new KeyValueText("level1", "123111");
            lvl2 = new KeyValueText("level2", "123111");
            lvl3 = new KeyValueText("level3", "123111");

            buttonDetail = new Button("seeDetails");

            AddChild(name, lvl1, lvl2, lvl3, buttonDetail);

            UpdateTexts();
        }

        public void UpdateTexts()
        {
            name.SetText(city.Name);
            lvl1.SetValue(city.GetNumberOfCitizensOfLevel(DistrictType.Suburb).ToString());
            lvl2.SetValue(city.GetNumberOfCitizensOfLevel(DistrictType.City).ToString());
            lvl3.SetValue(city.GetNumberOfCitizensOfLevel(DistrictType.Business).ToString());
        }
                        
    }
}
