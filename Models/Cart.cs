using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TibiaMarche_WebApp.Models
{
    public class Cart
    {
        private int _id;
        private Buyer _buyer;
        private List<Item> _items = new();
        private DateTimeOffset? _expiresAt;
        // TODO look into what type of datetime object we should use.

        // Parameterless constructor (model binding / Firestore)
        public Cart()
        {
            _buyer = null!;
        }

        public Cart(Buyer buyer)
        {
            _buyer = buyer;
        }

        public Cart(Buyer buyer, List<Item> items, DateTimeOffset expiresAt)
        {
            _buyer = buyer;
            _items = items ?? new List<Item>();
            _expiresAt = expiresAt;
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
            set { _items = value == null ? new List<Item>() : new List<Item>(value); }
        }

        public DateTimeOffset? ExpiresAt
        {
            get { return _expiresAt; }
            set { _expiresAt = value; }
        }

        public int Size
        {
            get { return _items.Count; }
        }

        public void AddItem(Item item)
        {
            if (_items.Count == 0)
            {
                // set expiry -- TODO figure out this system
            }

            item.AddToCart();
            _items.Add(item);
        }

        public bool RemoveItem(Item item)
        {
            if (!_items.Remove(item))
            {
                return false;
            }
            item.RemoveFromCart();
            return true;
        }

        public void ClearCart()
        {
            foreach (Item item in _items.ToList())   // iterate a copy: can't modify a list while looping over it
            {
                RemoveItem(item);
            }
        }

        public bool IsEmpty()
        {
            return _items.Count == 0;
        }

    }
}