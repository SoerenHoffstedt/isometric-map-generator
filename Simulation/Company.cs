using Industry.World;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Industry.Simulation
{
    public class Company
    {
        private Simulator simulator;
        public int displayMoney;
        public List<Store> stores;
        private readonly bool isUserCompany;

        private int money;        
        private int stdWorkersForNewStore = 3;

        public Company(Simulator simulator, bool isUserCompany)
        {
            this.simulator = simulator;
            this.isUserCompany = isUserCompany;
            stores = new List<Store>(16);
            money = displayMoney = 10000;
        }

        public bool IsUserCompany()
        {
            return isUserCompany;
        }

        public Store AddStore(Point p)
        {
            Store store = new Store(p, stdWorkersForNewStore, this, simulator);
            stores.Add(store);
            ChangeMoney(-1000);

            for (int i = 0; i < stdWorkersForNewStore; i++)
                simulator.AddAgentFunction(store);

            return store;
        }

        public void ChangeMoney(int amount)
        {
            money += amount;
            if(isUserCompany)
            {                
                Scenes.GameScene.tweener.Tween(this, new { displayMoney = money }, 1f).Ease(Glide.Ease.SineOut);
            }
        }

        public void DeliveryFinished(PizzaOrder currentDelivery)
        {
            ChangeMoney(currentDelivery.price);
            
        }        

        public void WeekFinish()
        {            
            int costs = 0;
            foreach(Store s in stores)
            {
                costs += s.GetWeeksInvoice();
                s.CalculateWeeksIncome();
            }
            
            ChangeMoney(-costs);
            Debug.WriteLine("Company weeks final invoice: " + costs);
        }

        #region Statistics

        public double GetAvgDeliveryTime()
        {
            double avg = 0.0;
            foreach(Store s in stores)
            {
                avg += s.GetAvgDeliveryTime();
            }
            return avg / stores.Count;
        }

        public double GetAvgReputation()
        {
            double rep = 0.0;
            foreach(Store s in stores)
            {
                rep += s.GetReputation();
            }
            return rep / stores.Count;
        }

        public double GetAvgPizzaQuality()
        {
            double qual = 0.0;
            foreach (Store s in stores)
            {
                qual += s.GetPizzaQuality();
            }
            return qual / stores.Capacity;
        }

        #endregion

    }
}
