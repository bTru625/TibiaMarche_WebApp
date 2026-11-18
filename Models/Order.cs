using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TibiaMarche_WebApp.Models
{
    public class Order
    {
        private int _id;
        private Buyer _buyer = null!;       // TODO - should we connect this to a buyer more directly?
        private List<Item> _items = new();
        private double _total;
        private Currency _currency;
        private double _exchangeRate;       // the exchange rate at time of purchase
        private DateTime _orderDate;
        private string _paymentId = string.Empty;
        private DateTime? _deliveryDate = null;

        // Parameterless constructor (model binding / Firestore)
        public Order()
        {
        }

        public Order(Buyer buyer, List<Item> items, double total, Currency currency,
                     double exchangeRate, DateTime orderDate, string paymentId, DateTime? deliveryDate)
        {
            _buyer = buyer;
            _items = items;
            _total = total;
            _currency = currency;
            _exchangeRate = exchangeRate;
            _orderDate = orderDate;
            _paymentId = paymentId;
            _deliveryDate = deliveryDate;
        }

        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public Buyer Buyer
        {
            get { return _buyer; }
            set { _buyer = value; }
        }

        public List<Item> Items
        {
            get { return _items; }
            set { _items = value; }
        }

        public double Total
        {
            get { return _total; }
            set { _total = value; }
        }

        public Currency Currency
        {
            get { return _currency; }
            set { _currency = value; }
        }

        public double ExchangeRate
        {
            get { return _exchangeRate; }
            set { _exchangeRate = value; }
        }

        public DateTime OrderDate
        {
            get { return _orderDate; }
            set { _orderDate = value; }
        }

        public string PaymentId
        {
            get { return _paymentId; }
            set { _paymentId = value; }
        }

        public DateTime? DeliveryDate
        {
            get { return _deliveryDate; }
            set { _deliveryDate = value; }
        }

        public bool IsAllDelivered()
        {
            foreach (Item item in _items)
            {
                if (!item.IsDelivered())
                {
                    return false;
                }
            }
            return true;
        }
    }
}