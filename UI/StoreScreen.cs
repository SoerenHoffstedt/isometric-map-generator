using BarelyUI;
using BarelyUI.Layouts;
using Industry.Simulation;
using Microsoft.Xna.Framework;
using System;


namespace Industry.UI
{
    public class StoreScreen : HorizontalLayout
    {
        Store store;
        float updateTimer;
        KeyValueText deliveredPizzas;
        KeyValueText outStandingOrders;
        KeyValueText avgDeliveryTime;
        KeyValueText deliveryEmployeesCount;

        KeyValueText lastWeeksIncome;
        Text costs;
        KeyValueText weeklyRent;
        KeyValueText weeklyEmployeeWage;
        KeyValueText weeklyTotalCost;

        Action<Store> HireAgentFunction;
        Action<Store> FireAgentFunction;

        public StoreScreen(Action<Store> HireAgentFunction, Action<Store> FireAgentFunction) : base()
        {
            Layout.PushLayout("storeScreen");
            this.HireAgentFunction = HireAgentFunction;
            this.FireAgentFunction = FireAgentFunction;

            Padding = new Point(10, 10);
            Margin = 10;
            SetFixedSize(700, 500);

            VerticalLayout leftSide = new VerticalLayout();
            leftSide.SetFixedWidth(300);

            deliveredPizzas         = new KeyValueText("deliveredPizzas", "0");
            outStandingOrders       = new KeyValueText("outstandingOrders", "0");
            avgDeliveryTime         = new KeyValueText("avgDeliveryTime", "5");
            deliveryEmployeesCount  = new KeyValueText("deliveryEmployees", "3");

            HorizontalLayout buttonsHor = new HorizontalLayout();            
            Button hireButton = new Button("hireEmployee");
            Button fireButton = new Button("fireEmployee");
            hireButton.OnMouseClick = () => { HireAgentFunction(store); UpdateTexts(); };
            fireButton.OnMouseClick = () => { FireAgentFunction(store); UpdateTexts(); };
            buttonsHor.AddChild(hireButton, fireButton);            

            lastWeeksIncome     = new KeyValueText("lastWeekIncome", "0");

            costs               = new Text("costs");
            weeklyRent          = new KeyValueText("rent", "0");
            weeklyEmployeeWage  = new KeyValueText("employees", "0");
            weeklyTotalCost     = new KeyValueText("total", "0");


            leftSide.AddChild(deliveryEmployeesCount);
            leftSide.AddChild(deliveredPizzas);
            leftSide.AddChild(outStandingOrders);
            leftSide.AddChild(avgDeliveryTime);
            leftSide.AddChild(buttonsHor);
            leftSide.AddChild(lastWeeksIncome);
            leftSide.AddChild(costs);
            leftSide.AddChild(weeklyRent);
            leftSide.AddChild(weeklyEmployeeWage);
            leftSide.AddChild(weeklyTotalCost);


            AddChild(leftSide);
            Layout.PopLayout("storeScreen");
        }


        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            if(isOpen && store != null)
            {
                updateTimer += deltaTime;
                if(updateTimer >= 0.5f)
                {
                    UpdateTexts();
                    updateTimer = 0f;
                }
            }
        }

        public void SetStore(Store store)
        {
            this.store = store;
            UpdateTexts();
        }

        public Store GetStore()
        {
            return store;
        }

        private void UpdateTexts()
        {
            deliveredPizzas.SetValue($"{store.GetDeliveredPizzas()}");
            outStandingOrders.SetValue($"{store.GetCurrentOutstandingOrderCount()}");
            avgDeliveryTime.SetValue(String.Format("{0:0.00}", store.GetAvgDeliveryTime()));
            deliveryEmployeesCount.SetValue(store.GetDeliveryEmployeeCount().ToString());

            int rent = store.GetRent();
            int wages = store.GetWages();

            weeklyRent.SetValue(rent.ToString());
            weeklyEmployeeWage.SetValue(wages.ToString());
            weeklyTotalCost.SetValue((rent + wages).ToString());

            lastWeeksIncome.SetValue(store.GetLastWeeksIncome().ToString());

        }



    }
}
