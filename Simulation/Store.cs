using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Industry.Agents;
using System.Diagnostics;

namespace Industry.Simulation
{
    public class Store
    {
        private Simulator simulator;
        public Company company;
        public Point tilePosition;
        private List<Agent> employees;
        private Queue<PizzaOrder> orders;

        private int rent = 200;
        public int minOrdersPerDelivery = 1;

        private bool takeOutsideCityOrders = true;
        private int deliveredPizzas;
        private double totalDeliveryTime;
        private int totalIncome;
        private int lastWeeksIncome;
        private int thisWeeksIncome;
        private double reputation;
        private double pizzaQuality;

        private int employeesToFire = 0;        


        public Store(Point position, int newEmplyoees, Company company, Simulator simulator)
        {
            this.simulator = simulator;
            this.company = company;
            tilePosition = position;
            employees = new List<Agent>();
            orders = new Queue<PizzaOrder>();
        }

        public Store(XmlNode saveXml)
        {
            employees = new List<Agent>();
        }

        public void AddEmployeeAgent(Agent a)
        {
            employees.Add(a);
            EmployeeIsBack(a);
        }
        
        public void FireAnEmployee()
        {
            employeesToFire += 1;
        }

        public void PlaceOrder(Point deliverTo, int price, double currentSimTime)
        {
            orders.Enqueue(new PizzaOrder(deliverTo, price, currentSimTime));
        }

        public void DeliveryFinished(PizzaOrder currentDelivery)
        {
            deliveredPizzas++;
            totalDeliveryTime += simulator.simTime - currentDelivery.createdOnSimTime;
            totalIncome += currentDelivery.price;
            thisWeeksIncome += currentDelivery.price;
            company.DeliveryFinished(currentDelivery);            
        }

        public void EmployeeIsBack(Agent a)
        {
            Debug.Assert(a.state == AgentState.Idle);
            if(employeesToFire > 0)
            {
                employees.Remove(a);
                employeesToFire -= 1;
                simulator.RemoveAgentFunction(a);
            }
            CheckForStartDelivery();
        }      
        
        public void CheckForStartDelivery()
        {
            if (orders.Count < minOrdersPerDelivery)
                return;

            foreach(Agent availAgent in IdleEmployees())
            {
                if(availAgent.state == AgentState.Idle)
                {
                    int cap = availAgent.deliveryCapacity;
                    int added = 0;
                    for (int i = 0; i < cap && orders.Count > 0; i++)
                    {
                        availAgent.TakeOrder(orders.Dequeue());
                        added++;
                    }
                    Debug.WriteLine($"Added orders to agent: {added}");
                }
            }

        }

        private IEnumerable<Agent> IdleEmployees()
        {
            for(int i = 0; i < employees.Count; i++)
            {
                if (employees[i].state == AgentState.Idle)
                    yield return employees[i];
            }
        }

        public void CalculateWeeksIncome()
        {
            lastWeeksIncome = thisWeeksIncome;
            thisWeeksIncome = 0;
        }

        public int GetWeeksInvoice()
        {
            int invoice = rent;

            foreach(Agent a in employees)
            {
                invoice += a.baseWage;
            }

            return invoice;
        }

        private void CalculateReputation()
        {

        }

        #region Stats

        public int GetRent()
        {
            return rent;
        }

        public int GetWages()
        {
            int wages = 0;
            foreach(Agent a in employees)
            {
                wages += a.baseWage;
            }
            return wages;
        }

        public int GetLastWeeksIncome()
        {
            return lastWeeksIncome;
        }

        public double GetReputation()
        {
            return reputation;
        }

        public double GetPizzaQuality()
        {
            return pizzaQuality;
        }

        public int GetDeliveredPizzas()
        {
            return deliveredPizzas;
        }

        public int GetCurrentOutstandingOrderCount()
        {
            return orders.Count;
        }

        public double GetAvgDeliveryTime()
        {
            if (deliveredPizzas == 0)
                return 0.0;
            else
                return totalDeliveryTime / deliveredPizzas;
        }

        public int GetDeliveryEmployeeCount()
        {
            return employees.Count - employeesToFire;
        }

        public IEnumerable<Point> IterateOutstandingOrders()
        {
            foreach(PizzaOrder o in orders)
            {
                yield return o.deliverTo;
            }
        }

        #endregion
    }
}
