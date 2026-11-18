using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TibiaMarche_WebApp.Models
{

    public class Admin : User
    {
        private List<Item> _items;

        protected Admin(string id, string email) : base(id, email)
        {
            _items = new List<Item>();
        }

        public Admin(string id, string email, List<Item> items) : base(id, email)
        {
            _items = items;
        }

        public List<Item> Items
        {
            get { return _items; }
            set { _items = value; }
        }

        public int GetGoldPaid(Item item)
        {
            return item.GoldPaid;
        }

        public void SetGoldPaid(Item item, int goldPaid)
        {
            item.GoldPaid = goldPaid;
        }

    }
}