using Glide;
using Industry.Agents;
using Industry.World;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Industry.Simulation
{
    public class Simulator
    {
        public static Tweener tweener = new Tweener();
        private Random random;
        private SimSpeed simSpeed = SimSpeed.Normal;
        public double simTime { get; private set; }

        private const double tickTimeCreateOrders = 30.0f;
        private double tickTimerCreateOrders      = 15.0f;

        public List<City> cities;
        public List<Company> competitorCompanies;
        public Company playerCompany;

        public Action<Store> AddAgentFunction { get; private set; }

        public Action<Agent> RemoveAgentFunction { get; private set; }

        public int week { get; private set; }
        public int day { get; private set; }
        private double dayProgress;
        private readonly double timePerDay = 60.0;

        public Simulator(int competitors, Map map, Action<Store> AddAgentFunction, Action<Agent> RemoveAgentFunction)
        {
            week = 1;
            day = 1;
            dayProgress = 0.0f;
            this.AddAgentFunction = AddAgentFunction;
            this.RemoveAgentFunction = RemoveAgentFunction;
            competitorCompanies   = new List<Company>(competitors);
            for (int i = 0; i < competitors; i++)
            {
                competitorCompanies.Add(new Company(this, false));
            }

            playerCompany   = new Company(this, true);

            cities          = new List<City>(map.cityRooms.Count);
            foreach (World.Generation.Room r in map.cityRooms)
            {
                cities.Add(new City(map.tiles, r));
            }

            random = new Random();
        }

        public Simulator(XmlNode saveXml)
        {

        }

        public void Update(double deltaTime)
        {
            if (simSpeed == SimSpeed.Paused)
                return;

            double factor = GetSimSpeedFactor(); 
            deltaTime *= factor;
            simTime += deltaTime;

            dayProgress += deltaTime;
            if(dayProgress >= timePerDay)
            {
                dayProgress -= timePerDay;
                NextDay();
            }

            tickTimerCreateOrders += deltaTime;
            if(tickTimerCreateOrders >= tickTimeCreateOrders)
            {
                CreateOrders();
                tickTimerCreateOrders = 0.0f;
            }

            //dont do this every frame?
            foreach(Store store in IterateAllStores())
            {
                store.CheckForStartDelivery();
            }

            tweener.Update((float)deltaTime);
        }

        #region time

        private double GetSimSpeedFactor()
        {
            switch (simSpeed)
            {
                default:
                case SimSpeed.Paused:
                    return 0.0;
                case SimSpeed.Normal:
                    return 1.0;
                case SimSpeed.Fast:
                    return 4.0;
                case SimSpeed.Faster:
                    return 8.0;
            }
        }

        private void NextDay()
        {
            day++;
            if(day > 7)
            {
                week++;
                day = 1;
                NewWeek();
            }
        }

        private void NewWeek()
        {
            //TODO Endabrechnung and show finance UI for player
            foreach(Company c in IterateAllCompanies())
            {
                c.WeekFinish();
            }
        }

        #endregion


        #region Sim Functions

        private void CreateOrders()
        {
            Debug.WriteLine("Create Orders");
            //right now orders can only be given to stores in the same city
            foreach(City city in cities)
            {
                if (city.stores.Count == 0)
                    continue;

                int orders = city.Citizens / 10;
                Debug.WriteLine($"Orders city: {orders}");
                int run = -1;
                int next = random.Next(city.Citizens);

                while (orders > 0)
                {

                    foreach (Point to in city.buildingTiles)
                    {
                        run++;

                        if (run == next)
                        {
                            Store nearestShop = FindStoreForOrder(city, to);
                            if (nearestShop == null)
                            {
                                orders = 0;
                                break;
                            }

                            nearestShop.PlaceOrder(to, 10, simTime);
                            orders--;
                            run = -1;
                            next = random.Next(city.Citizens);
                            if (orders < 1)
                                break;
                        }

                    }

                }


            }

        }

        // TODO: take statistics and stuff into consideration, not just the shortest distance
        // maybe rank all companies and if the best company has a store that could potentially 
        // deliver under a time threshold that store will be selected.
        //
        private Store FindStoreForOrder(City city, Point deliverTo)
        {
            Store toReturn = null;
            float shortestDist = float.MaxValue;


            foreach(Store s in city.stores.Values)
            {            
            //foreach(Store s in IterateAllStores())
            //{
                float dist = (s.tilePosition - deliverTo).ToVector2().Length();
                if(dist < shortestDist)
                {
                    shortestDist = dist;
                    toReturn = s;
                }
            }

            return toReturn;
        }


        private IEnumerable<Store> IterateAllStores()
        {
            foreach (Store s in playerCompany.stores)
                yield return s;
            foreach(Company c in competitorCompanies)
            {
                foreach (Store s in c.stores)
                    yield return s;
            }
        }

        #endregion        

        #region UI API

        public void ChangeSimSpeed(SimSpeed newSpeed)
        {
            simSpeed = newSpeed;
        }

        public SimSpeed GetSimSpeed()
        {
            return simSpeed;
        }

        public IEnumerable<Point> IterateOutstandingOrders()
        {
            
            foreach(Store s in playerCompany.stores)
            {
                foreach(Point p in s.IterateOutstandingOrders())
                {
                    yield return p;
                }
            }
            /*foreach(Company c in competitorCompanies)
            {
                foreach (Store s in c.stores)
                {
                    foreach (Point p in s.IterateOutstandingOrders())
                    {
                        yield return p;
                    }
                }
            }*/
        }        

        public IEnumerable<Company> IterateAllCompanies()
        {
            yield return playerCompany;
            foreach (Company c in competitorCompanies)
                yield return c;
        }

        public double GetDayProgress()
        {
            return dayProgress / timePerDay;
        }

        #endregion

    }
}
