using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TibiaMarche_WebApp.Models
{
    public class Item
    {

        private int _id;
        private string _name = string.Empty;
        private string _img = string.Empty;
        private double _price;
        private Server _server = null!;
        private string _description = string.Empty;
        private int _level;
        private int _goldPaid;
        private string? _deliverTo = null;
        private Vocation _vocation;
        private Status _status;

        // Parameterless constructor (model binding / Firestore)
        public Item()
        {
        }

        public Item(string name,
                    string img,
                    double price,
                    Server server,
                    string description,
                    int level,
                    int goldPaid,
                    Vocation vocation,
                    Status status)
        {
            _name = name;
            _img = img;
            _price = price;
            _server = server;
            _description = description;
            _level = level;
            _goldPaid = goldPaid;
            _deliverTo = null;   // no deliverTo on creation, unless you plan to add items that are already sold to use the app as a delivery reminder
            _vocation = vocation;
            _status = status;
        }

        // Constructor with API_Item (will this be used?)
        public Item(API_Item apiItem,
                    double price,
                    Server server,
                    int level,
                    int goldPaid,
                    Vocation vocation,
                    Status status)
        {
            _name = apiItem.Name;
            _img = apiItem.Img;
            _price = price;
            _server = server;
            _description = apiItem.Description;
            _level = level;
            _goldPaid = goldPaid;
            _deliverTo = null;   // no deliverTo on creation
            _vocation = vocation;
            _status = status;
        }

        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Img
        {
            get { return _img; }
            set { _img = value; }
        }

        public double Price
        {
            get { return _price; }
            set { _price = value; }
        }

        public Server Server
        {
            get { return _server; }
            set { _server = value; }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public int Level
        {
            get { return _level; }
            set { _level = value; }
        }

        public int GoldPaid
        {
            get { return _goldPaid; }
            set { _goldPaid = value; }
        }

        public string? DeliverTo
        {
            get { return _deliverTo; }
            set { _deliverTo = value; }
        }

        public Vocation Vocation
        {
            get { return _vocation; }
            set { _vocation = value; }
        }

        public Status Status
        {
            get { return _status; }
            set { _status = value; }
        }

        public bool CanBeUsedBy(Vocation vocation)
        {
            return _vocation == Vocation.ANY || _vocation == vocation;
        }

        public bool IsAvailable()
        {
            return _status.IsAvailable();
        }

        public bool IsDelivered()
        {
            return _status == Status.Delivered;
        }

        // Methods that do actions to an item

        private void RequiredStatus(Status expected, string action)
        {
            if (_status != expected)
            {
                throw new InvalidOperationException(
                    $"Cannot {action}: item is {_status.DisplayName()} but must be {expected.DisplayName()}");
            }
        }

        public void AddToCart()
        {
            RequiredStatus(Status.Available, "add to cart");
            _status = Status.In_Cart;
        }

        public void RemoveFromCart()
        {
            RequiredStatus(Status.In_Cart, "remove from cart");
            _status = Status.Available;
            _deliverTo = null;
        }

        public void MarkSold()
        {
            RequiredStatus(Status.In_Cart, "mark sold");
            if (string.IsNullOrWhiteSpace(_deliverTo))
            {
                throw new InvalidOperationException("Cannot mark sold: deliverTo is required.");
            }
            _status = Status.Sold;
        }

        public void MarkDelivered()
        {
            RequiredStatus(Status.Sold, "mark delivered");
            _status = Status.Delivered;
        }

    }
}