using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TibiaMarche_WebApp.Models
{
    public class Buyer : User
    {
        private Currency _currency;
        private Cart? _cart;
        private List<Order> _orders = new();

        // Constructor, CAD by default
        public Buyer(string id, string email) : base(id, email)
        {
            _currency = Currency.CAD;
        }

        // Constructor, other currency
        public Buyer(string id, string email, Currency currency) : base(id, email)
        {
            _currency = currency;
        }

        public Currency Currency
        {
            get { return _currency; }
            set { _currency = value; }
        }

        public Cart? Cart
        {
            get { return _cart; }
            set { _cart = value; }
        }

        public List<Order> Orders
        {
            get { return _orders; }
            set { _orders = value ?? new List<Order>(); }
        }

        public double ConvertPrice(double priceCAD)
        {
            return _currency.ConvertFromCAD(priceCAD);
        }

        public void ClearCart()
        {
            _cart?.ClearCart();
        }

        public Order? GetOrder(int id)
        {
            foreach (Order order in _orders)
            {
                if (order.Id == id)
                {
                    return order;
                }
            }
            return null;
        }

        public void AddOrder(Order order)
        {
            _orders.Add(order);
        }


    }
}